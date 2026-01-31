-- ============================================================================
-- Database: YummyZoomDb
-- Description: Clears all data from public schema tables
-- IMPORTANT: This script must be run on the YummyZoomDb database
-- ============================================================================

DO $$
DECLARE
    t RECORD;
    sql text;
    remaining_rows bigint := 0;
    cnt bigint;
    current_db text;
BEGIN
    -- Verify we're connected to the correct database
    SELECT current_database() INTO current_db;
    
    IF current_db != 'YummyZoomDb' THEN
        RAISE EXCEPTION 'Wrong database! Connected to "%" but script requires "YummyZoomDb". Please connect to YummyZoomDb first.', current_db;
    END IF;
    
    RAISE NOTICE 'Confirmed: Running on database "%"', current_db;
    -- Build TRUNCATE statement for public tables
    SELECT 'TRUNCATE TABLE ' ||
           string_agg(format('%I.%I', schemaname, tablename), ', ') ||
           ' RESTART IDENTITY CASCADE'
    INTO sql
    FROM pg_tables
    WHERE schemaname = 'public';

    -- If there are no tables, just confirm and exit
    IF sql IS NULL THEN
        RAISE NOTICE 'No tables found in public schema. Nothing to wipe.';
        RETURN;
    END IF;

    -- 1) Wipe
    EXECUTE sql;

    -- 2) Verify with exact counts
    FOR t IN
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'public'
    LOOP
        EXECUTE format('SELECT count(*) FROM public.%I', t.tablename) INTO cnt;
        remaining_rows := remaining_rows + cnt;
    END LOOP;

    -- 3) Confirm
    IF remaining_rows = 0 THEN
        RAISE NOTICE 'Public schema wipe successful: all public tables are empty.';
    ELSE
        RAISE EXCEPTION 'Public schema wipe incomplete: % rows still exist.', remaining_rows;
    END IF;
END $$;