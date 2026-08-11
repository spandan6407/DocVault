import { useCallback, useEffect, useMemo, useState } from "react";
import DashboardLayout from "../components/layout/DashboardLayout";
import DocumentCard from "../components/DocumentCard";
import DocumentEditor from "../components/DocumentEditor";
import DocumentViewer from "../components/DocumentViewer";
import { useAuth } from "../context/useAuth";
import { docApi, userApi } from "../api/api";
import {
    Button,
    EmptyState,
    Field,
    Input,
    Label,
    List,
    PageHeader,
    PageTitle,
    Section,
    SectionTitle,
} from "../styles/shared";

export default function UserDashboard() {
    const { user } = useAuth();

    // User can belong to multiple projects — use first membership's projectId.
    const projectId = useMemo(
        () => user.projects?.[0]?.projectId ?? null,
        [user.projects]
    );

    const [documents, setDocuments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [viewingDoc, setViewingDoc] = useState(null);

    const [file, setFile] = useState(null);
    const [uploadTitle, setUploadTitle] = useState("");
    const [uploading, setUploading] = useState(false);

    const [requestedProjectId, setRequestedProjectId] = useState("");
    const [requestSent, setRequestSent] = useState(false);

    const loadDocuments = useCallback(async () => {
        if (!projectId) return;
        setLoading(true);
        try {
            const res = await docApi.get(`/projects/${projectId}/documents`);
            setDocuments(res.data);
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        queueMicrotask(loadDocuments);
    }, [loadDocuments]);

    const handleUpload = useCallback(
        async (e) => {
            e.preventDefault();
            if (!file) return;
            setUploading(true);
            try {
                const form = new FormData();
                form.append("File", file);
                form.append("Title", uploadTitle || file.name);
                form.append("ProjectId", projectId);
                await docApi.post("/documents", form, {
                    headers: { "Content-Type": "multipart/form-data" },
                });
                setFile(null);
                setUploadTitle("");
                loadDocuments();
            } finally {
                setUploading(false);
            }
        },
        [file, uploadTitle, projectId, loadDocuments]
    );

    const handleRequestChange = useCallback(
        async (e) => {
            e.preventDefault();
            await userApi.post("/users/project-change-request", {
                RequestedProjectId: requestedProjectId,
            });
            setRequestSent(true);
        },
        [requestedProjectId]
    );

    const ownDocumentIds = useMemo(
        () => new Set(documents.filter((d) => d.createdBy === user.id).map((d) => d.id)),
        [documents, user.id]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>My Project</PageTitle>
            </PageHeader>

            <Section>
                <SectionTitle>Upload a document</SectionTitle>
                <form onSubmit={handleUpload}>
                    <Field>
                        <Label htmlFor="upload-title">Title</Label>
                        <Input
                            id="upload-title"
                            value={uploadTitle}
                            onChange={(e) => setUploadTitle(e.target.value)}
                            placeholder="Document title"
                        />
                    </Field>
                    <Field>
                        <Label htmlFor="upload-file">File</Label>
                        <input id="upload-file" type="file" onChange={(e) => setFile(e.target.files[0])} required />
                    </Field>
                    <Button type="submit" disabled={uploading || !file}>
                        {uploading ? "Uploading..." : "Upload"}
                    </Button>
                </form>
            </Section>

            <Section>
                <SectionTitle>Write a document</SectionTitle>
                <DocumentEditor onCreated={loadDocuments} />
            </Section>

            <Section id="documents">
                <SectionTitle>Documents</SectionTitle>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : documents.length === 0 ? (
                    <EmptyState>No documents in this project yet.</EmptyState>
                ) : (
                    <List>
                        {documents.map((doc) => (
                            <li key={doc.id}>
                                <DocumentCard
                                    document={doc}
                                    canEdit={ownDocumentIds.has(doc.id)}
                                    canDelete={ownDocumentIds.has(doc.id)}
                                    onChanged={loadDocuments}
                                    onView={setViewingDoc}
                                />
                            </li>
                        ))}
                    </List>
                )}
            </Section>

            <Section>
                <SectionTitle>Request a project change</SectionTitle>
                {requestSent ? (
                    <EmptyState>Request submitted — an admin will review it.</EmptyState>
                ) : (
                    <form onSubmit={handleRequestChange}>
                        <Field>
                            <Label htmlFor="req-project">Requested project ID</Label>
                            <Input
                                id="req-project"
                                value={requestedProjectId}
                                onChange={(e) => setRequestedProjectId(e.target.value)}
                                required
                            />
                        </Field>
                        <Button type="submit">Submit request</Button>
                    </form>
                )}
            </Section>

            {viewingDoc && <DocumentViewer document={viewingDoc} onClose={() => setViewingDoc(null)} />}
        </DashboardLayout>
    );
}