info: YummyZoom.Application.Search.Queries.UniversalSearch.UniversalSearchQuery[0]
      YummyZoom Request: UniversalSearchQuery  Unknown UniversalSearchQuery { Term = Ý, Latitude = , Longitude = , OpenNow = , Cuisines = System.String[], Tags = System.String[], PriceBands = System.Int16[], EntityTypes = System.String[], Bbox = , Sort = relevance, IncludeFacets = False, PageNumber = 1, PageSize = 20 }
fail: YummyZoom.Application.Restaurants.Queries.SearchRestaurants.SearchRestaurantsQuery[0]
      YummyZoom Request: Unhandled Exception for Request SearchRestaurantsQuery SearchRestaurantsQuery { Q = , Cuisine = , Lat = , Lng = , RadiusKm = , PageNumber = 1, PageSize = 20, MinRating = , Sort = relevance, Bbox = , Tags = System.Collections.Generic.List`1[System.String], TagIds = System.Collections.Generic.List`1[System.Guid], DiscountedOnly = , IncludeFacets = False }
      YummyZoom.Application.Common.Exceptions.ValidationException: One or more validation failures have occurred.
         at YummyZoom.Application.Common.Behaviours.ValidationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\ValidationBehaviour.cs:line 32
         at YummyZoom.Application.Common.Behaviours.UnhandledExceptionBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\UnhandledExceptionBehaviour.cs:line 18
fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
      An unhandled exception has occurred while executing the request.
      YummyZoom.Application.Common.Exceptions.ValidationException: One or more validation failures have occurred.
         at YummyZoom.Application.Common.Behaviours.ValidationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\ValidationBehaviour.cs:line 32
         at YummyZoom.Application.Common.Behaviours.UnhandledExceptionBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\UnhandledExceptionBehaviour.cs:line 18
         at YummyZoom.Web.Endpoints.Restaurants.<>c.<<Map>b__0_39>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Web\Endpoints\Restaurants.cs:line 790
      --- End of stack trace from previous location ---
         at Microsoft.AspNetCore.Http.RequestDelegateFactory.ExecuteTaskResult[T](Task`1 task, HttpContext httpContext)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.<Invoke>g__Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)
info: YummyZoom.Application.Search.Queries.Autocomplete.AutocompleteQuery[0]
      YummyZoom Request: AutocompleteQuery  Unknown AutocompleteQuery { Term = p, Limit = 10, Types = System.String[] }
fail: YummyZoom.Application.Search.Queries.Autocomplete.AutocompleteQuery[0]
      YummyZoom Request: Unhandled Exception for Request AutocompleteQuery AutocompleteQuery { Term = p, Limit = 10, Types = System.String[] }
      Npgsql.PostgresException (0x80004005): 42P08: could not determine data type of parameter $3
      
      POSITION: 197
         at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
         at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
         at Dapper.SqlMapper.QueryAsync[T](IDbConnection cnn, Type effectiveType, CommandDefinition command) in /_/Dapper/SqlMapper.Async.cs:line 434
         at YummyZoom.Application.Search.Queries.Autocomplete.AutocompleteQueryHandler.Handle(AutocompleteQuery request, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Search\Queries\Autocomplete\AutocompleteQueryHandler.cs:line 56
         at YummyZoom.Application.Common.Behaviours.LoggingBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\LoggingBehaviour.cs:line 31
         at YummyZoom.Application.Common.Behaviours.PerformanceBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\PerformanceBehaviour.cs:line 28
         at YummyZoom.Application.Common.Behaviours.CachingBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\CachingBehaviour.cs:line 23
         at YummyZoom.Application.Common.Behaviours.AuthorizationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\AuthorizationBehaviour.cs:line 89
         at YummyZoom.Application.Common.Behaviours.ValidationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\ValidationBehaviour.cs:line 35
         at YummyZoom.Application.Common.Behaviours.UnhandledExceptionBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\UnhandledExceptionBehaviour.cs:line 18
        Exception data:
          Severity: ERROR
          SqlState: 42P08
          MessageText: could not determine data type of parameter $3
          Position: 197
          File: parse_param.c
          Line: 307
          Routine: check_parameter_resolution_walker
fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
      An unhandled exception has occurred while executing the request.
      Npgsql.PostgresException (0x80004005): 42P08: could not determine data type of parameter $3
      
      POSITION: 197
         at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
         at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
         at Dapper.SqlMapper.QueryAsync[T](IDbConnection cnn, Type effectiveType, CommandDefinition command) in /_/Dapper/SqlMapper.Async.cs:line 434
         at YummyZoom.Application.Search.Queries.Autocomplete.AutocompleteQueryHandler.Handle(AutocompleteQuery request, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Search\Queries\Autocomplete\AutocompleteQueryHandler.cs:line 56
         at YummyZoom.Application.Common.Behaviours.LoggingBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\LoggingBehaviour.cs:line 31
         at YummyZoom.Application.Common.Behaviours.PerformanceBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\PerformanceBehaviour.cs:line 28
         at YummyZoom.Application.Common.Behaviours.CachingBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\CachingBehaviour.cs:line 23
         at YummyZoom.Application.Common.Behaviours.AuthorizationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\AuthorizationBehaviour.cs:line 89
         at YummyZoom.Application.Common.Behaviours.ValidationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\ValidationBehaviour.cs:line 35
         at YummyZoom.Application.Common.Behaviours.UnhandledExceptionBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\UnhandledExceptionBehaviour.cs:line 18
         at YummyZoom.Web.Endpoints.Search.<>c.<<Map>b__0_1>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Web\Endpoints\Search.cs:line 50
      --- End of stack trace from previous location ---
         at Microsoft.AspNetCore.Http.RequestDelegateFactory.ExecuteTaskResult[T](Task`1 task, HttpContext httpContext)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.<Invoke>g__Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)
        Exception data:
          Severity: ERROR
          SqlState: 42P08
          MessageText: could not determine data type of parameter $3
          Position: 197
          File: parse_param.c
          Line: 307
          Routine: check_parameter_resolution_walker
fail: Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware[1]
      An unhandled exception has occurred while executing the request.
      System.InvalidOperationException: The exception handler configured on ExceptionHandlerOptions produced a 404 status response. This InvalidOperationException containing the original exception was thrown since this is often due to a misconfigured ExceptionHandlingPath. If the exception handler is expected to return 404 status responses then set AllowStatusCode404Response to true.
       ---> Npgsql.PostgresException (0x80004005): 42P08: could not determine data type of parameter $3
      
      POSITION: 197
         at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
         at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
         at Dapper.SqlMapper.QueryAsync[T](IDbConnection cnn, Type effectiveType, CommandDefinition command) in /_/Dapper/SqlMapper.Async.cs:line 434
         at YummyZoom.Application.Search.Queries.Autocomplete.AutocompleteQueryHandler.Handle(AutocompleteQuery request, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Search\Queries\Autocomplete\AutocompleteQueryHandler.cs:line 56
         at YummyZoom.Application.Common.Behaviours.LoggingBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\LoggingBehaviour.cs:line 31
         at YummyZoom.Application.Common.Behaviours.PerformanceBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\PerformanceBehaviour.cs:line 28
         at YummyZoom.Application.Common.Behaviours.CachingBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\CachingBehaviour.cs:line 23
         at YummyZoom.Application.Common.Behaviours.AuthorizationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\AuthorizationBehaviour.cs:line 89
         at YummyZoom.Application.Common.Behaviours.ValidationBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\ValidationBehaviour.cs:line 35
         at YummyZoom.Application.Common.Behaviours.UnhandledExceptionBehaviour`2.Handle(TRequest request, RequestHandlerDelegate`1 next, CancellationToken cancellationToken) in E:\source\repos\CA\YummyZoom\src\Application\Common\Behaviours\UnhandledExceptionBehaviour.cs:line 18
         at YummyZoom.Web.Endpoints.Search.<>c.<<Map>b__0_1>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Web\Endpoints\Search.cs:line 50
      --- End of stack trace from previous location ---
         at Microsoft.AspNetCore.Http.RequestDelegateFactory.ExecuteTaskResult[T](Task`1 task, HttpContext httpContext)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.<Invoke>g__Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)
        Exception data:
          Severity: ERROR
          SqlState: 42P08
          MessageText: could not determine data type of parameter $3
          Position: 197
          File: parse_param.c
          Line: 307
          Routine: check_parameter_resolution_walker
         --- End of inner exception stack trace ---
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.HandleException(HttpContext context, ExceptionDispatchInfo edi)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.HandleException(HttpContext context, ExceptionDispatchInfo edi)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.<Invoke>g__Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)
         at NSwag.AspNetCore.Middlewares.SwaggerUiIndexMiddleware.Invoke(HttpContext context)
         at NSwag.AspNetCore.Middlewares.RedirectToIndexMiddleware.Invoke(HttpContext context)
         at NSwag.AspNetCore.Middlewares.OpenApiDocumentMiddleware.Invoke(HttpContext context)
         at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)
         at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)
         at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)