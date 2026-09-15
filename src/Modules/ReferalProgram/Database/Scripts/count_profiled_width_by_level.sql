-- Counts profiled and system places by level for one marketing structure.
-- Run in pgAdmin Query Tool while connected to the Programs database.
-- Edit the three values in params before executing the script.

WITH params AS
(
    SELECT
        'MARKETING_ADDRESS'::text AS marketing_addr,
        1::smallint AS structure_number,
        6::bigint AS level
)
SELECT COUNT(*) AS profiled_width
FROM public.places place
CROSS JOIN params
WHERE place.marketing_addr = params.marketing_addr
  AND place.structure_number = params.structure_number
  AND place.deep = params.level
  AND place.profile_addr IS NOT NULL;

-- Optional overview for every level of the same marketing structure.
WITH params AS
(
    SELECT
        'MARKETING_ADDRESS'::text AS marketing_addr,
        1::smallint AS structure_number
)
SELECT
    place.deep AS level,
    COUNT(*) FILTER (
        WHERE place.profile_addr IS NOT NULL
    ) AS profiled_width,
    COUNT(*) FILTER (
        WHERE place.profile_addr IS NULL
    ) AS system_width,
    COUNT(*) AS total_width
FROM public.places place
CROSS JOIN params
WHERE place.marketing_addr = params.marketing_addr
  AND place.structure_number = params.structure_number
GROUP BY place.deep
ORDER BY place.deep;
