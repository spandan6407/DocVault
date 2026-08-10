import { useCallback, useEffect, useState } from "react";
import DashboardLayout from "../components/layout/DashboardLayout";
import DocumentCard from "../components/DocumentCard";
import DocumentEditor from "../components/DocumentEditor";
import DocumentViewer from "../components/DocumentViewer";
import { useAuth } from "../context/useAuth";
import { docApi, userApi } from "../api/api";
import {
    Badge,
    Button,
    EmptyState,
    Field,
    Input,
    Label,
    List,
    ListRow,
    PageHeader,
    PageTitle,
    Section,
    SectionTitle,
} from "../styles/shared";

export default function ProjectHeadDashboard() {
    const { user } = useAuth();
    const [documents, setDocuments] = useState([]);
    const [members, setMembers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [viewingDoc, setViewingDoc] = useState(null);

    const [file, setFile] = useState(null);
    const [uploadTitle, setUploadTitle] = useState("");
    const [uploading, setUploading] = useState(false);

    const [requestedProjectId, setRequestedProjectId] = useState("");
    const [requestSent, setRequestSent] = useState(false);

    const loadAll = useCallback(async () => {
        setLoading(true);
        try {
            const [docsRes, membersRes] = await Promise.all([
                docApi.get(`/projects/${user.projectId}/documents`),
                userApi.get(`/projects/${user.projectId}/users`),
            ]);
            setDocuments(docsRes.data);
            setMembers(membersRes.data);
        } finally {
            setLoading(false);
        }
    }, [user.projectId]);

    useEffect(() => {
        queueMicrotask(loadAll);
    }, [loadAll]);

    const handleUpload = useCallback(
        async (e) => {
            e.preventDefault();
            if (!file) return;
            setUploading(true);
            try {
                const form = new FormData();
                form.append("File", file);
                form.append("Title", uploadTitle || file.name);
                form.append("ProjectId", user.projectId);
                await docApi.post("/documents", form, {
                    headers: { "Content-Type": "multipart/form-data" },
                });
                setFile(null);
                setUploadTitle("");
                loadAll();
            } finally {
                setUploading(false);
            }
        },
        [file, uploadTitle, user.projectId, loadAll]
    );

    const handleRemoveMember = useCallback(
        async (memberId) => {
            if (!window.confirm("Remove this member from the project?")) return;
            await userApi.delete(`/users/${memberId}`);
            loadAll();
        },
        [loadAll]
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

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Project Head Dashboard</PageTitle>
            </PageHeader>

            <Section>
                <SectionTitle>Upload a document</SectionTitle>
                <form onSubmit={handleUpload}>
                    <Field>
                        <Label htmlFor="ph-upload-title">Title</Label>
                        <Input
                            id="ph-upload-title"
                            value={uploadTitle}
                            onChange={(e) => setUploadTitle(e.target.value)}
                        />
                    </Field>
                    <Field>
                        <Label htmlFor="ph-upload-file">File</Label>
                        <input id="ph-upload-file" type="file" onChange={(e) => setFile(e.target.files[0])} required />
                    </Field>
                    <Button type="submit" disabled={uploading || !file}>
                        {uploading ? "Uploading..." : "Upload"}
                    </Button>
                </form>
            </Section>

            <Section>
                <SectionTitle>Write a document</SectionTitle>
                <DocumentEditor onCreated={loadAll} />
            </Section>

            <Section id="documents">
                <SectionTitle>Project documents</SectionTitle>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : documents.length === 0 ? (
                    <EmptyState>No documents yet.</EmptyState>
                ) : (
                    <List>
                        {documents.map((doc) => (
                            <li key={doc.id}>
                                {/* ProjectHead can manage any document in their own project */}
                                <DocumentCard
                                    document={doc}
                                    canEdit
                                    canDelete
                                    onChanged={loadAll}
                                    onView={setViewingDoc}
                                />
                            </li>
                        ))}
                    </List>
                )}
            </Section>

            <Section id="members">
                <SectionTitle>Members</SectionTitle>
                {members.length === 0 ? (
                    <EmptyState>No members yet.</EmptyState>
                ) : (
                    <List>
                        {members.map((m) => (
                            <ListRow key={m.id}>
                                <div>
                                    <strong>{m.firstName} {m.lastName}</strong>{" "}
                                    <Badge>{m.role}</Badge>
                                    <div style={{ color: "#6B778C", fontSize: 12 }}>{m.email}</div>
                                </div>
                                {m.id !== user.id && (
                                    <Button $variant="danger" onClick={() => handleRemoveMember(m.id)}>
                                        Remove
                                    </Button>
                                )}
                            </ListRow>
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
                            <Label htmlFor="ph-req-project">Requested project ID</Label>
                            <Input
                                id="ph-req-project"
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