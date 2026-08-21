import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import DocumentCard from "../../components/DocumentCard";
import DocumentViewer from "../../components/DocumentViewer";
import { useAuth } from "../../context/useAuth";
import { docApi } from "../../api/api";
import { Button, EmptyState, List, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function ProjectDocumentsPage() {
    const { projectId } = useParams();
    const { user } = useAuth();
    const navigate = useNavigate();

    const [documents, setDocuments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [viewingDoc, setViewingDoc] = useState(null);

    const membership = useMemo(() => (user?.projects || []).find((p) => String(p.projectId) === String(projectId)), [user, projectId]);
    const isHead = membership?.role === "ProjectHead";

    const loadDocs = useCallback(async () => {
        setLoading(true);
        try {
            const res = await docApi.get(`/projects/${projectId}/documents`);
            setDocuments(res.data || []);
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        queueMicrotask(loadDocs);
    }, [loadDocs]);

    const handleUploadNav = () => navigate(`/projects/${projectId}/upload`);
    const handleWriteNav = () => navigate(`/projects/${projectId}/write`);

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Project Documents</PageTitle>
                <div style={{ display: "flex", gap: 8 }}>
                    <Button onClick={handleUploadNav}>Upload Document</Button>
                    <Button $variant="secondary" onClick={handleWriteNav}>Write Document</Button>
                </div>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading documents...</EmptyState>
                ) : documents.length === 0 ? (
                    <EmptyState>No documents found in this project.</EmptyState>
                ) : (
                    <List>
                        {documents.map((doc) => {
                            const isOwner = String(doc.createdBy) === String(user.id);
                            const canEdit = isHead || isOwner;
                            const canDelete = isHead || isOwner;
                            return (
                                <li key={doc.id}>
                                    <DocumentCard
                                        document={doc}
                                        canEdit={canEdit}
                                        canDelete={canDelete}
                                        onChanged={loadDocs}
                                        onView={setViewingDoc}
                                    />
                                </li>
                            );
                        })}
                    </List>
                )}
            </Section>

            {viewingDoc && <DocumentViewer document={viewingDoc} onClose={() => setViewingDoc(null)} />}
        </DashboardLayout>
    );
}

