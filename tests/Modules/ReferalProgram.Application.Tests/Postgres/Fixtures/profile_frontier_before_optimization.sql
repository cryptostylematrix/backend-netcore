-- Frozen pre-optimization query for differential tests; do not update to match production.
WITH scoped AS MATERIALIZED
(
    SELECT
        parent.*,
        COUNT(child.id) FILTER (
            WHERE child.profile_addr IS NOT NULL
        )::bigint AS profiled_child_count
    FROM public.places parent
    LEFT JOIN public.places child
      ON child.parent_id = parent.id
     AND child.profile_addr IS NOT NULL
    WHERE parent.marketing_addr = @marketingAddr
      AND parent.structure_number = @structureNumber
      AND parent.mp LIKE @mpPrefix
    GROUP BY parent.id
),
profiled_scoped AS MATERIALIZED
(
    SELECT
        scoped.*,
        ROW_NUMBER() OVER (
            PARTITION BY scoped.deep
            ORDER BY scoped.mp ASC, scoped.id ASC
        ) AS horizontal_index,
        COUNT(*) OVER (
            PARTITION BY scoped.deep
        ) AS horizontal_count
    FROM scoped
    WHERE scoped.profile_addr IS NOT NULL
),
eligible AS
(
    SELECT
        scoped.*,
        target_level.profiled_count AS target_level_profiled_count,
        current_level.profiled_count AS current_level_profiled_count,
        ARRAY(
            SELECT
            (
                SELECT COUNT(*)::bigint
                FROM scoped descendant
                WHERE descendant.profile_addr IS NOT NULL
                  AND descendant.mp LIKE
                      left(scoped.mp, path_length) || '%'
            )
            FROM generate_series(
                char_length(@rootMp) + 8,
                char_length(scoped.mp),
                8
            ) AS path(path_length)
            ORDER BY path_length
        ) AS branch_load
    FROM profiled_scoped scoped
    CROSS JOIN LATERAL
    (
        SELECT
            COUNT(*)::bigint AS profiled_count,
            MIN(level_place.profiled_child_count) FILTER (
                WHERE level_place.is_active = true
                  AND level_place.kind <> 2
                  AND (@width = 0 OR level_place.filling < @width)
            ) AS minimum_profiled_child_count
        FROM profiled_scoped level_place
        WHERE level_place.deep = scoped.deep
    ) current_level
    CROSS JOIN LATERAL
    (
        SELECT COUNT(*)::bigint AS profiled_count
        FROM scoped level_place
        WHERE level_place.profile_addr IS NOT NULL
          AND level_place.deep = scoped.deep + 1
    ) target_level
    WHERE scoped.profile_addr IS NOT NULL
      AND scoped.is_active = true
      AND scoped.kind <> 2
      AND (@width = 0 OR scoped.filling < @width)
      AND (
          target_level.profiled_count < GREATEST(
              @profiledWidthLimit,
              current_level.profiled_count
          )
          OR scoped.profiled_child_count = 0
      )
      AND (
          current_level.profiled_count < @profiledWidthLimit
          OR scoped.profiled_child_count = 0
      )
      AND scoped.profiled_child_count
          = current_level.minimum_profiled_child_count
      AND NOT EXISTS
      (
          SELECT 1
          FROM unnest(@lockMps) AS locks(lock_mp)
          WHERE lower(scoped.mp || lpad(to_hex(scoped.filling + 1), 8, '0'))
              LIKE lower(lock_mp) || '%'
      )
),
candidates AS
(
    SELECT *
    FROM eligible
    ORDER BY
        deep ASC,
        profiled_child_count ASC,
        CASE
            WHEN horizontal_index <= (horizontal_count + 1) / 2
                THEN horizontal_index * 2 - 1
            ELSE (horizontal_count - horizontal_index + 1) * 2
        END ASC,
        branch_load ASC,
        mp ASC,
        id ASC
    LIMIT 1
)
SELECT
    id                    AS "Id",
    parent_id             AS "ParentId",
    mp                    AS "Mp",
    pos_group             AS "PosGroup",
    marketing_addr        AS "MarketingAddr",
    structure_number      AS "StructNumber",
    profile_addr          AS "ProfileAddr",
    place_number          AS "PlaceNumber",
    profile_login         AS "ProfileLogin",
    "index"               AS "Index",
    parent_profile_addr   AS "ParentProfileAddr",
    parent_profile_login  AS "ParentProfileLogin",
    parent_place_number   AS "ParentPlaceNumber",
    created_at            AS "CreatedAt",
    activated_at          AS "ActivatedAt",
    is_active             AS "IsActive",
    kind                  AS "Kind",
    pos                   AS "Pos",
    filling               AS "Filling",
    deep                  AS "Deep"
FROM candidates;
