import { useCallback, useEffect, useState } from "react";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { userApi } from "../../api/api";
import { EmptyState, List, ListRow, PageHeader, PageTitle, Section, IconButton } from "../../styles/shared";
import { FaCheck, FaTimes } from 'react-icons/fa';

export default function RequestsPage() {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [projects, setProjects] = useState([]);

    const loadRequests = useCallback(async () => {
        setLoading(true);
        try {
            const res = await userApi.get("/project-change-requests");
            setRequests(res.data);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        queueMicrotask(loadRequests);
    }, [loadRequests]);

    useEffect(() => {
        let mounted = true;
        (async () => {
            try {
                const res = await userApi.get('/user-projects');
                if (!mounted) return;
                setProjects((res.data || []).map(p => ({ id: p.projectId, name: p.projectName })));
            } catch (err) {
                console.warn('Failed to load projects for requests', err);
            }
        })();
        return () => { mounted = false; };
    }, []);

    const handleApprove = useCallback(
        async (id) => {
            await userApi.put(`/project-change-requests/${id}/approve`);
            loadRequests();
        },
        [loadRequests]
    );

    const handleReject = useCallback(
        async (id) => {
            await userApi.put(`/project-change-requests/${id}/reject`);
            loadRequests();
        },
        [loadRequests]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Pending Project Change Requests</PageTitle>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : requests.length === 0 ? (
                    <EmptyState>No pending requests.</EmptyState>
                ) : (
                    <List>
                        {requests.map((r) => (
                            <ListRow key={r.id}>
                                <div>
                                    {r.userFullName} → project {projects.find(p => p.id === r.requestedProjectId)?.name || r.requestedProjectId}
                                </div>
                                <div style={{ display: "flex", gap: 8 }}>
                                    <IconButton aria-label="Approve" onClick={() => handleApprove(r.id)}>
                                        <FaCheck style={{ color: '#10B981' }} />
                                    </IconButton>
                                    <IconButton aria-label="Reject" onClick={() => handleReject(r.id)}>
                                        <FaTimes style={{ color: '#EF4444' }} />
                                    </IconButton>
                                </div>
                            </ListRow>
                        ))}
                    </List>
                )}
            </Section>
        </DashboardLayout>
    );
}