import { memo, useCallback, useState } from "react";
import styled from "styled-components";
import { docApi } from "../api/api";
import { Button, Input } from "../styles/shared";

const Row = styled.div`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 16px;
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
  background: ${(p) => p.theme.color.surface};
`;

const Info = styled.div`
  min-width: 0;
`;

const Title = styled.div`
  font-size: 14px;
  font-weight: 600;
`;

const Meta = styled.div`
  font-size: 12px;
  color: ${(p) => p.theme.color.textMuted};
  margin-top: 2px;
`;

const Actions = styled.div`
  display: flex;
  gap: 8px;
  flex-shrink: 0;
`;

const EditForm = styled.form`
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
`;

function DocumentCard({ document, canEdit, canDelete, onChanged, onView }) {
    const [editing, setEditing] = useState(false);
    const [title, setTitle] = useState(document.title);
    const [description, setDescription] = useState(document.description || "");
    const [busy, setBusy] = useState(false);

    const handleView = useCallback(async () => {
        if (onView) {
            onView(document);
            return;
        }
        const res = await docApi.get(`/documents/${document.id}/download`, { responseType: "blob" });
        const url = URL.createObjectURL(res.data);
        window.open(url, "_blank");
    }, [document, onView]);

    const handleDownload = useCallback(async () => {
        const res = await docApi.get(`/documents/${document.id}/download`, { responseType: "blob" });
        const url = URL.createObjectURL(res.data);
        const link = window.document.createElement("a");
        link.href = url;
        link.download = document.fileName;
        link.click();
        URL.revokeObjectURL(url);
    }, [document.id, document.fileName]);

    const handleDelete = useCallback(async () => {
        if (!window.confirm(`Delete "${document.title}"? This can't be undone.`)) return;
        setBusy(true);
        try {
            await docApi.delete(`/documents/${document.id}`);
            onChanged?.();
        } finally {
            setBusy(false);
        }
    }, [document.id, document.title, onChanged]);

    const handleSaveEdit = useCallback(
        async (e) => {
            e.preventDefault();
            setBusy(true);
            try {
                const form = new FormData();
                form.append("Title", title);
                form.append("Description", description);
                await docApi.put(`/documents/${document.id}`, form, {
                    headers: { "Content-Type": "multipart/form-data" },
                });
                setEditing(false);
                onChanged?.();
            } finally {
                setBusy(false);
            }
        },
        [document.id, title, description, onChanged]
    );

    if (editing) {
        return (
            <Row>
                <EditForm onSubmit={handleSaveEdit}>
                    <Input value={title} onChange={(e) => setTitle(e.target.value)} required />
                    <Input
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Description"
                    />
                    <Actions>
                        <Button type="submit" disabled={busy}>
                            {busy ? "Saving..." : "Save"}
                        </Button>
                        <Button type="button" $variant="secondary" onClick={() => setEditing(false)}>
                            Cancel
                        </Button>
                    </Actions>
                </EditForm>
            </Row>
        );
    }

    return (
        <Row>
            <Info>
                <Title>{document.title}</Title>
                <Meta>
                    {document.fileName} · {document.description || "No description"}
                </Meta>
            </Info>
            <Actions>
                <Button $variant="secondary" onClick={handleView}>
                    View
                </Button>
                <Button $variant="secondary" onClick={handleDownload}>
                    Download
                </Button>
                {canEdit && (
                    <Button $variant="secondary" onClick={() => setEditing(true)}>
                        Edit
                    </Button>
                )}
                {canDelete && (
                    <Button $variant="danger" onClick={handleDelete} disabled={busy}>
                        Delete
                    </Button>
                )}
            </Actions>
        </Row>
    );
}

export default memo(DocumentCard);








