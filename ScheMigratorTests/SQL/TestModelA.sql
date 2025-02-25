CREATE SCHEMA IF NOT EXISTS public;

CREATE TEMP TABLE IF NOT EXISTS temp_table_info AS
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_schema = 'public' 
AND table_name = 'testmodela';

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'testmodela') THEN
        CREATE TABLE public.testmodela (
            id integer
        );
    END IF;
END $$;


DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM temp_table_info WHERE column_name = 'id') THEN
        ALTER TABLE public.testmodela
        ADD COLUMN id integer;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM temp_table_info
        WHERE column_name = 'id'
        AND (
            data_type != 'integer'::regtype::text
            OR is_nullable = 'NO' != false
        )
    ) THEN
        ALTER TABLE public.testmodela
        ALTER COLUMN id TYPE integer USING id::integer,
        ALTER COLUMN id DROP NOT NULL;
    END IF;
END $$;

DO $$
DECLARE
    _col record;
BEGIN
    FOR _col IN
        SELECT column_name FROM temp_table_info
        WHERE column_name NOT IN ('id')
    LOOP
        EXECUTE 'ALTER TABLE public.testmodela DROP COLUMN ' || quote_ident(_col.column_name);
    END LOOP;
END $$;

DROP TABLE temp_table_info;
