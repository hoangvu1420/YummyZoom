using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Configuration;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Http.ImageProxy;

public sealed class HttpImageProxyService : IImageProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ImageProxyOptions _options;
    private readonly ILogger<HttpImageProxyService> _logger;

    public HttpImageProxyService(
        IHttpClientFactory httpClientFactory,
        IOptions<ImageProxyOptions> options,
        ILogger<HttpImageProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<ProxiedImage>> GetAsync(Uri url, CancellationToken cancellationToken = default)
    {
        // Basic URL validation
        if (url is null || (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !_options.AllowHttp))
        {
            return Result.Failure<ProxiedImage>(Error.Validation("ImageProxy.InvalidUrl", "Only HTTPS image URLs are allowed."));
        }

        if (string.IsNullOrWhiteSpace(url.Host))
        {
            return Result.Failure<ProxiedImage>(Error.Validation("ImageProxy.InvalidUrl", "URL host is required."));
        }

        // Allowlist enforcement
        if (!IsHostAllowlisted(url.Host))
        {
            return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.ForbiddenHost", "The requested host is not allowlisted."));
        }

        // SSRF: resolve and block private networks if enabled
        if (_options.BlockPrivateNetworks)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(url.DnsSafeHost);
                foreach (var ip in addresses)
                {
                    if (IsPrivateOrReserved(ip))
                    {
                        return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.PrivateAddress", "Target resolves to a private or reserved IP."));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DNS resolution failed for {Host}", url.DnsSafeHost);
                return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.DnsFailed", "Could not resolve target host."));
            }
        }

        var client = _httpClientFactory.CreateClient("ImageProxy");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.Timeout", "Timed out fetching the image."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching image from {Url}", url);
            return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.FetchFailed", "Failed to fetch the image."));
        }

        if (!response.IsSuccessStatusCode)
        {
            var code = (int)response.StatusCode;
            response.Dispose();
            return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.UpstreamStatus", $"Upstream returned status {code}."));
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!_options.AllowedContentTypes.Any(t => contentType.StartsWith(t, StringComparison.OrdinalIgnoreCase))
            && !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            response.Dispose();
            return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.UnsupportedType", $"Unsupported content type '{contentType}'."));
        }

        var length = response.Content.Headers.ContentLength;
        if (length.HasValue && length.Value > _options.MaxBytes)
        {
            response.Dispose();
            return Result.Failure<ProxiedImage>(Error.Problem("ImageProxy.TooLarge", $"Image exceeds maximum allowed size of {_options.MaxBytes} bytes."));
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        // Wrap with limiter if no Content-Length or could still exceed during stream
        Stream finalStream = new LimitedReadStream(stream, _options.MaxBytes, onExceeded: () =>
        {
            try { response.Dispose(); } catch { /* ignored */ }
        });

        var etag = response.Headers.ETag?.Tag;
        var lastModified = response.Content.Headers.LastModified;

        // NOTE: HttpResponseMessage will be disposed when stream finishes reading (onExceeded handled).
        // We intentionally do not dispose here to allow streaming to proceed.

        return Result.Success(new ProxiedImage(finalStream, contentType, length, etag, lastModified));
    }

    private bool IsHostAllowlisted(string host)
    {
        if (_options.AllowedHosts is null || _options.AllowedHosts.Length == 0)
        {
            return false; // secure default
        }

        foreach (var entry in _options.AllowedHosts)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;
            var e = entry.Trim().Trim('.');
            if (entry.StartsWith('.'))
            {
                if (host.EndsWith('.' + e, StringComparison.OrdinalIgnoreCase) || string.Equals(host, e, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(host, entry, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (b[0] == 10) return true;
            // 172.16.0.0/12
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            // 192.168.0.0/16
            if (b[0] == 192 && b[1] == 168) return true;
            // 169.254.0.0/16 (link-local)
            if (b[0] == 169 && b[1] == 254) return true;
            // 100.64.0.0/10 (carrier-grade NAT)
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
            if (ip.Equals(IPAddress.IPv6Loopback)) return true;
        }
        return false;
    }

    private sealed class LimitedReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private readonly Action? _onExceeded;
        private long _read;

        public LimitedReadStream(Stream inner, long maxBytes, Action? onExceeded)
        {
            _inner = inner;
            _maxBytes = maxBytes;
            _onExceeded = onExceeded;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _inner.Read(buffer, offset, count);
            AfterRead(n);
            return n;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var n = await _inner.ReadAsync(buffer, cancellationToken);
            AfterRead(n);
            return n;
        }
        public override int ReadByte()
        {
            var b = _inner.ReadByte();
            AfterRead(b < 0 ? 0 : 1);
            return b;
        }
        private void AfterRead(int n)
        {
            _read += n;
            if (_read > _maxBytes)
            {
                _onExceeded?.Invoke();
                throw new IOException("Image exceeds maximum allowed size.");
            }
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

