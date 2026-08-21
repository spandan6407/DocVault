import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../components/layout/DashboardLayout";
import DocumentViewer from "../components/DocumentViewer";
import { docApi, userApi } from "../api/api";
import {
    Badge,
    Button,
    EmptyState,
    Grid,
    Input,
    List,
    ListRow,
    PageHeader,
    PageTitle,
    Section,
    SectionTitle,
    StatCard,
    StatLabel,
    StatNumber,
} from "../styles/shared";

export default function AdminDashboard() {
    const navigate = useNavigate();
    const [projectCount, setProjectCount] = useState(null);
    const [userCount, setUserCount] = useState(null);
    const [viewingDoc, setViewingDoc] = useState(null);

    const [query, setQuery] = useState("");
    const [searching, setSearching] = useState(false);
    const [results, setResults] = useState(null);

    // giving the count for no of project and no of users 
    const loadCounts = useCallback(async () => {
        const [projectsRes, usersRes] = await Promise.all([docApi.get("/projects"), userApi.get("/users")]);
        setProjectCount(projectsRes.data.length);
        setUserCount(usersRes.data.length);
    }, []);

    useEffect(() => {
        queueMicrotask(loadCounts);
    }, [loadCounts]);


    // this is not working properly, need to check the api and the backend for this
    const handleSearch = useCallback(
        async (e) => {
            e.preventDefault();
            if (!query.trim()) return;
            setSearching(true);
            try {
                const res = await docApi.post("/ai/search", { Query: query });
                setResults(res.data);
            } finally {
                setSearching(false);
            }
        },
        [query]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Admin Dashboard</PageTitle>
            </PageHeader>

            <Grid $cols="1fr 1fr" style={{ marginBottom: 20 }}>
                <StatCard type="button" onClick={() => navigate("/admin/projects")}>
                    <StatLabel>Projects</StatLabel>
                    <StatNumber>{projectCount ?? "…"}</StatNumber>
                </StatCard>
                <StatCard type="button" onClick={() => navigate("/admin/users")}>
                    <StatLabel>Users</StatLabel>
                    <StatNumber>{userCount ?? "…"}</StatNumber>
                </StatCard>
            </Grid>

            <Section id="ai-search">
                <SectionTitle>AI Search (across all projects)</SectionTitle>
                <form onSubmit={handleSearch} style={{ display: "flex", gap: 8, marginBottom: 12 }}>
                    <Input
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        placeholder="e.g. employee onboarding"
                        style={{ flex: 1 }}
                    />
                    <Button type="submit" disabled={searching}>
                        {searching ? "Searching..." : "Search"}
                    </Button>
                </form>
                {results && (
                    <List>
                        {results.length === 0 ? (
                            <EmptyState>No relevant documents found.</EmptyState>
                        ) : (
                            results.map((r) => (
                                <ListRow
                                    key={r.documentId}
                                    style={{ cursor: "pointer" }}
                                    onClick={() => setViewingDoc({ id: r.documentId, title: r.title, fileName: r.title })}
                                >
                                    <div>
                                        <strong>{r.title}</strong>
                                        <div style={{ color: "#6B778C", fontSize: 12 }}>{r.summary}</div>
                                    </div>
                                    <Badge>{r.score}/10</Badge>
                                </ListRow>
                            ))
                        )}
                    </List>
                )}
            </Section>

            {viewingDoc && <DocumentViewer document={viewingDoc} onClose={() => setViewingDoc(null)} />}
        </DashboardLayout>
    );
}