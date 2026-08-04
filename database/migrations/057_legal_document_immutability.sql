-- v6.10.2: enforce the legal publication invariant at the database boundary.
BEGIN;

CREATE OR REPLACE FUNCTION habitflow.prevent_published_legal_version_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF OLD.status = 'Published' AND (
    NEW.version IS DISTINCT FROM OLD.version OR NEW.locale IS DISTINCT FROM OLD.locale OR
    NEW.title IS DISTINCT FROM OLD.title OR NEW.summary IS DISTINCT FROM OLD.summary OR
    NEW.sanitized_content IS DISTINCT FROM OLD.sanitized_content OR NEW.content_hash IS DISTINCT FROM OLD.content_hash OR
    NEW.effective_at IS DISTINCT FROM OLD.effective_at OR NEW.requires_reacceptance IS DISTINCT FROM OLD.requires_reacceptance
  ) THEN
    RAISE EXCEPTION 'published legal document versions are immutable' USING ERRCODE = 'check_violation';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_legal_version_immutable ON habitflow.legal_document_versions;
CREATE TRIGGER trg_legal_version_immutable
BEFORE UPDATE ON habitflow.legal_document_versions
FOR EACH ROW EXECUTE FUNCTION habitflow.prevent_published_legal_version_mutation();

COMMIT;
