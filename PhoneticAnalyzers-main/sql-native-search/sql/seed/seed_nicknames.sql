-- Starter nickname mappings (extend offline via dictionary/LLM precomputation)
INSERT INTO nickname_maps (canonical_name, nickname, is_bidirectional)
VALUES
  ('JOHN', 'JOHNNY', TRUE),
  ('JOHN', 'JON', TRUE),
  ('JOHN', 'JACK', TRUE),
  ('WILLIAM', 'BILL', TRUE),
  ('WILLIAM', 'WILL', TRUE),
  ('ROBERT', 'BOB', TRUE),
  ('ROBERT', 'ROB', TRUE),
  ('ELIZABETH', 'LIZ', TRUE),
  ('ELIZABETH', 'BETH', TRUE),
  ('MARGARET', 'PEGGY', TRUE)
ON CONFLICT (canonical_name, nickname) DO NOTHING;
