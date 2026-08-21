import { useCallback, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import { Button, EmptyState, Field, Input, Label, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function UploadPage() {
    const { projectId } = useParams();
    const navigate = useNavigate();

    const [file, setFile] = useState(null);
    const [title, setTitle] = useState("");
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState(null);

    const handleSubmit = useCallback(
        async (e) => {
            e.preventDefault();
            if (!file) return;
            setUploading(true);
            setError(null);
            try {
                const form = new FormData();
                form.append("File", file);
                form.append("Title", title || file.name);
                form.append("ProjectId", projectId);
                // Do not manually set Content-Type; let the browser/axios set the correct
                // multipart boundary header. Manually setting it can cause a 400 Bad Request
                // from the server because the boundary is missing.
                await docApi.post("/documents", form);
                navigate(`/projects/${projectId}/documents`);
            } catch (err) {
                // Surface backend validation errors when available
                const data = err?.response?.data;
                if (!data) {
                    setError("Upload failed. Please try again.");
                } else if (data.errors) {
                    // ASP.NET ModelState validation -> { errors: { Field: [..] } }
                    const messages = Object.values(data.errors).flat();
                    setError(messages.join(" "));
                } else if (data.message) {
                    setError(data.message);
                } else {
                    setError(JSON.stringify(data));
                }
            } finally {
                setUploading(false);
            }
        },
        [file, title, projectId, navigate]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Upload Document</PageTitle>
            </PageHeader>

            <Section>
                <form onSubmit={handleSubmit}>
                    <Field>
                        <Label htmlFor="upload-title">Title</Label>
                        <Input id="upload-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Document title" />
                    </Field>
                    <Field>
                        <Label htmlFor="upload-file">File</Label>
                        <input id="upload-file" type="file" onChange={(e) => setFile(e.target.files[0])} required />
                    </Field>
                    {error && <EmptyState>{error}</EmptyState>}
                    <Button type="submit" disabled={uploading || !file}>
                        {uploading ? "Uploading..." : "Upload"}
                    </Button>
                </form>
            </Section>
        </DashboardLayout>
    );
}

