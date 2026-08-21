import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { userApi, docApi } from "../../api/api";
import { EmptyState, List, ListRow, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function MembersPage() {
    const { projectId } = useParams();
    const [members, setMembers] = useState([]);
    const [loading, setLoading] = useState(true);

    const loadMembers = useCallback(async () => {
        setLoading(true);
        try {
            let res;
            try {
                res = await userApi.get(`/projects/${projectId}/users`);
            } catch {
                res = await docApi.get(`/projects/${projectId}/members`);
            }
            setMembers(res.data || []);
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        queueMicrotask(loadMembers);
    }, [loadMembers]);

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Project Members</PageTitle>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading members...</EmptyState>
                ) : members.length === 0 ? (
                    <EmptyState>No members found.</EmptyState>
                ) : (
                    <List>
                        {members.map((m) => (
                            <ListRow key={m.id}>
                                <div>
                                    <strong>{m.firstName} {m.lastName}</strong>
                                    <div style={{ color: "#6B778C", fontSize: 12 }}>{m.email}</div>
                                </div>
                                <div style={{ fontWeight: 700 }}>{(m.projects || []).find(p => String(p.projectId) === String(projectId))?.role || "User"}</div>
                            </ListRow>
                        ))}
                    </List>
                )}
            </Section>
        </DashboardLayout>
    );
}

