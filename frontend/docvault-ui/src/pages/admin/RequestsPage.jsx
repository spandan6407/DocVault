import { useCallback, useEffect, useState } from "react";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { userApi } from "../../api/api";
import { Button, EmptyState, List, ListRow, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function RequestsPage() {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);

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
                                    {r.userFullName} → project {r.requestedProjectId}
                                </div>
                                <div style={{ display: "flex", gap: 8 }}>
                                    <Button onClick={() => handleApprove(r.id)}>Approve</Button>
                                    <Button $variant="danger" onClick={() => handleReject(r.id)}>
                                        Reject
                                    </Button>
                                </div>
                            </ListRow>
                        ))}
                    </List>
                )}
            </Section>
        </DashboardLayout>
    );
}