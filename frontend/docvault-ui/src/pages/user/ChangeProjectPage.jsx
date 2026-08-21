import { useCallback, useEffect, useState } from "react";
import { userApi, docApi } from "../../api/api";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { Button, EmptyState, List, ListRow, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function ChangeProjectPage() {
    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selected, setSelected] = useState(null);
    const [sent, setSent] = useState(false);

    useEffect(() => {
        let mounted = true;
        (async () => {
            try {
                const res = await docApi.get("/projects");
                if (mounted) setProjects(res.data || []);
            } finally {
                if (mounted) setLoading(false);
            }
        })();
        return () => (mounted = false);
    }, []);

    const handleRequest = useCallback(async () => {
        if (!selected) return;
        await userApi.post("/users/project-change-request", { RequestedProjectId: selected });
        setSent(true);
    }, [selected]);

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Change Project</PageTitle>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : sent ? (
                    <EmptyState>Request submitted — an admin will review it.</EmptyState>
                ) : projects.length === 0 ? (
                    <EmptyState>No projects available.</EmptyState>
                ) : (
                    <>
                        <List>
                            {projects.map((p) => (
                                <ListRow key={p.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                    <div>
                                        <strong>{p.name}</strong>
                                        <div style={{ color: "#6B778C", fontSize: 12 }}>{p.description}</div>
                                    </div>
                                    <Button $variant={selected === p.id ? "secondary" : undefined} onClick={() => setSelected(p.id)}>
                                        {selected === p.id ? "Selected" : "Select"}
                                    </Button>
                                </ListRow>
                            ))}
                        </List>
                        <div style={{ marginTop: 12 }}>
                            <Button onClick={handleRequest} disabled={!selected}>Submit request</Button>
                        </div>
                    </>
                )}
            </Section>
        </DashboardLayout>
    );
}

