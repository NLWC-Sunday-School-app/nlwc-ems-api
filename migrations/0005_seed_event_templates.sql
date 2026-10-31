-- Reference data: the organization and its three event templates. Editions are created in admin.
-- The Couples display name stays neutral until Q19 (Programme vs Retreat) is answered.
INSERT INTO organization(id, name) VALUES ('0192f5a0-0000-7000-8000-000000000001', 'NLWC');
INSERT INTO event_template(id, organization_id, code, name, config) VALUES
 ('0192f5a0-0000-7000-8000-0000000000b1', '0192f5a0-0000-7000-8000-000000000001', 'BB', 'Bible Bootcamp', '{}'),
 ('0192f5a0-0000-7000-8000-0000000000b2', '0192f5a0-0000-7000-8000-000000000001', 'SS', 'Singlz Summit', '{}'),
 ('0192f5a0-0000-7000-8000-0000000000b3', '0192f5a0-0000-7000-8000-000000000001', 'CP', 'Couples', '{}');
