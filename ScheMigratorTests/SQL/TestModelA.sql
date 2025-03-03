DO $$
DECLARE
    _col record;
BEGIN
CREATE SCHEMA IF NOT EXISTS public;

CREATE TEMP TABLE IF NOT EXISTS temp_table_info_public_test_model_a AS
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_schema = 'public' 
AND table_name = 'test_model_a';

CREATE TABLE IF NOT EXISTS public.test_model_a (            id integer PRIMARY KEY NOT NULL,
            name text NOT NULL,
            age integer NOT NULL
        );

ALTER TABLE public.test_model_a
ADD COLUMN IF NOT EXISTS id integer NOT NULL;

ALTER TABLE public.test_model_a
ADD COLUMN IF NOT EXISTS name text NOT NULL;

ALTER TABLE public.test_model_a
ADD COLUMN IF NOT EXISTS age integer NOT NULL;

IF EXISTS (
    SELECT 1 FROM temp_table_info_public_test_model_a
    WHERE column_name = 'id'
    AND (
        data_type <> 'integer'
        OR is_nullable = 'NO' <> true
    )
) THEN
    ALTER TABLE public.test_model_a
    ALTER COLUMN id TYPE integer USING id::integer,
    ALTER COLUMN id SET NOT NULL;
END IF;

IF EXISTS (
    SELECT 1 FROM temp_table_info_public_test_model_a
    WHERE column_name = 'name'
    AND (
        data_type <> 'text'
        OR is_nullable = 'NO' <> true
    )
) THEN
    ALTER TABLE public.test_model_a
    ALTER COLUMN name TYPE text USING name::text,
    ALTER COLUMN name SET NOT NULL;
END IF;

IF EXISTS (
    SELECT 1 FROM temp_table_info_public_test_model_a
    WHERE column_name = 'age'
    AND (
        data_type <> 'integer'
        OR is_nullable = 'NO' <> true
    )
) THEN
    ALTER TABLE public.test_model_a
    ALTER COLUMN age TYPE integer USING age::integer,
    ALTER COLUMN age SET NOT NULL;
END IF;

FOR _col IN (
    SELECT column_name FROM temp_table_info_public_test_model_a
    WHERE column_name NOT IN ('id', 'name', 'age')
)
LOOP
    EXECUTE format('ALTER TABLE %I.%I DROP COLUMN %I', 'public', 'test_model_a', _col.column_name);
END LOOP;


DROP TABLE IF EXISTS temp_table_info_public_test_model_a;
END $$;
