import { useState, useCallback, memo } from "react";
import styled from "styled-components";
import { docApi } from "../api/api";
import { useAuth } from "../context/useAuth";

// ---- styled-components ----
// Uses CSS custom properties so it inherits your existing theme tokens
// (define these vars once in styles/theme.css, e.g. on :root)

const Form = styled.form`
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-width: 640px;
  padding: 24px;
  background: var(--surface, #fff);
  border: 1px solid var(--border, #e2e2e2);
  border-radius: 10px;
`;

const Field = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
`;

const Label = styled.label`
  font-size: 13px;
  font-weight: 600;
  color: var(--text-muted, #555);
`;

const Input = styled.input`
  padding: 10px 12px;
  border: 1px solid ${(p) => (p.$invalid ? "var(--danger, #d64545)" : "var(--border, #ccc)")};
  border-radius: 6px;
  font-size: 14px;
  outline: none;

  &:focus {
    border-color: var(--accent, #3b6ef6);
    box-shadow: 0 0 0 3px var(--accent-soft, rgba(59, 110, 246, 0.15));
  }
`;

const TextArea = styled.textarea`
  padding: 10px 12px;
  border: 1px solid ${(p) => (p.$invalid ? "var(--danger, #d64545)" : "var(--border, #ccc)")};
  border-radius: 6px;
  font-size: 14px;
  font-family: inherit;
  resize: vertical;
  min-height: 180px;
  outline: none;

  &:focus {
    border-color: var(--accent, #3b6ef6);
    box-shadow: 0 0 0 3px var(--accent-soft, rgba(59, 110, 246, 0.15));
  }
`;

const ErrorText = styled.span`
  font-size: 12px;
  color: var(--danger, #d64545);
`;

const HelperText = styled.span`
  font-size: 12px;
  color: var(--text-muted, #888);
`;

const SubmitButton = styled.button`
  align-self: flex-start;
  padding: 10px 20px;
  background: var(--accent, #3b6ef6);
  color: #fff;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.15s ease;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &:hover:not(:disabled) {
    opacity: 0.9;
  }
`;

// ---- component ----

function DocumentEditor({ onCreated }) {
    const { user } = useAuth();

    const [values, setValues] = useState({ title: "", description: "", content: "" });
    const [errors, setErrors] = useState({});
    const [loading, setLoading] = useState(false);
    const [submitError, setSubmitError] = useState(null);

    const handleChange = useCallback((field) => (e) => {
        const value = e.target.value;
        setValues((prev) => ({ ...prev, [field]: value }));
        setErrors((prev) => (prev[field] ? { ...prev, [field]: null } : prev));
    }, []);

    const validate = useCallback((v) => {
        const next = {};
        if (!v.title.trim()) next.title = "Title is required.";
        if (!v.content.trim()) next.content = "Document content can't be empty.";
        return next;
    }, []);

    const handleSubmit = useCallback(
        async (e) => {
            e.preventDefault();
            const validationErrors = validate(values);
            setErrors(validationErrors);
            if (Object.keys(validationErrors).length > 0) return;

            setLoading(true);
            setSubmitError(null);
            try {
                await docApi.post("/documents/compose", {
                    Title: values.title,
                    Description: values.description,
                    Content: values.content,
                    ProjectId: user.projectId,
                });
                setValues({ title: "", description: "", content: "" });
                onCreated?.();
            } catch (err) {
                setSubmitError(
                    err?.response?.data?.message || "Couldn't create the document. Please try again."
                );
            } finally {
                setLoading(false);
            }
        },
        [values, user, validate, onCreated]
    );

    return (
        <Form onSubmit={handleSubmit} noValidate>
            <Field>
                <Label htmlFor="doc-title">Title</Label>
                <Input
                    id="doc-title"
                    placeholder="Document title"
                    value={values.title}
                    onChange={handleChange("title")}
                    $invalid={!!errors.title}
                    aria-invalid={!!errors.title}
                />
                {errors.title && <ErrorText>{errors.title}</ErrorText>}
            </Field>

            <Field>
                <Label htmlFor="doc-description">Description</Label>
                <Input
                    id="doc-description"
                    placeholder="Short description (optional)"
                    value={values.description}
                    onChange={handleChange("description")}
                />
            </Field>

            <Field>
                <Label htmlFor="doc-content">Content</Label>
                <TextArea
                    id="doc-content"
                    placeholder="Write your document content here..."
                    value={values.content}
                    onChange={handleChange("content")}
                    $invalid={!!errors.content}
                    aria-invalid={!!errors.content}
                />
                {errors.content && <ErrorText>{errors.content}</ErrorText>}
            </Field>

            {submitError && <ErrorText>{submitError}</ErrorText>}
            <HelperText>Documents are created under your current project.</HelperText>

            <SubmitButton type="submit" disabled={loading}>
                {loading ? "Creating..." : "Create Document"}
            </SubmitButton>
        </Form>
    );
}

export default memo(DocumentEditor);