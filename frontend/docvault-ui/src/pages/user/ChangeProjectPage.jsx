import { useCallback, useEffect, useState } from "react";
import { userApi, docApi } from "../../api/api";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { Button, EmptyState, List, ListRow, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function ChangeProjectPage() {
    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selected, setSelected] = useState(null);
    const [error, setError] = useState("");
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
        setError("");
        if (!selected) return;
        try {
            // Ensure we send the ID as a string
            await userApi.post("/users/project-change-request", { RequestedProjectId: String(selected) });
            setSent(true);
        } catch (err) {
            const msg = err?.response?.data?.message || err?.response?.data || err?.message || "Request failed.";
            setError(typeof msg === "string" ? msg : JSON.stringify(msg));
        }
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
                        {error && <div style={{ marginBottom: 12, color: "#b00020" }}>{error}</div>}
                        <List>
                            {projects.map((p) => (
                                <ListRow key={p.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                    <div>
                                        <strong>{p.name}</strong>
                                        <div style={{ color: "#6B778C", fontSize: 12 }}>{p.description}</div>
                                    </div>
                                    <Button $variant={selected === p.id ? "secondary" : undefined} onClick={() => setSelected(String(p.id))}>
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

