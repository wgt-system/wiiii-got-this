CREATE TABLE wgt_atlas_appearance (
    singleton_key INTEGER NOT NULL PRIMARY KEY CHECK (singleton_key = 1),
    theme TEXT NOT NULL CHECK (theme IN ('technical', 'elegant', 'machine', 'world'))
);
