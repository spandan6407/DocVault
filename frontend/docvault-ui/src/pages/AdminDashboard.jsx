import { useCallback, useEffect, useMemo, useState } from "react";
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
import { LineChart, Line, ResponsiveContainer, Tooltip } from 'recharts';

export default function AdminDashboard() {
    const navigate = useNavigate();
    const [projectCount, setProjectCount] = useState(null);
    const [userCount, setUserCount] = useState(null);
    const [projectsTrend, setProjectsTrend] = useState(null);
    const [usersTrend, setUsersTrend] = useState(null);
    const [viewingDoc, setViewingDoc] = useState(null);

    const [query, setQuery] = useState("");
    const [searching, setSearching] = useState(false);
    const [results, setResults] = useState(null);

    // giving the count for no of project and no of users 
    const loadCounts = useCallback(async () => {
        const [projectsRes, usersRes] = await Promise.all([docApi.get("/projects"), userApi.get("/users")]);
        setProjectCount(projectsRes.data.length);
        setUserCount(usersRes.data.length);

        // try to fetch trend/metrics endpoints; fall back to generated data
        try {
            const [projTrendRes, usersTrendRes] = await Promise.allSettled([
                docApi.get('/metrics/projects?points=8'),
                userApi.get('/metrics/users?points=8'),
            ]);

            if (projTrendRes.status === 'fulfilled' && Array.isArray(projTrendRes.value.data)) {
                setProjectsTrend(projTrendRes.value.data.map((v, i) => ({ name: String(i), value: v })));
            }
            if (usersTrendRes.status === 'fulfilled' && Array.isArray(usersTrendRes.value.data)) {
                setUsersTrend(usersTrendRes.value.data.map((v, i) => ({ name: String(i), value: v })));
            }
        } catch (err) {
            console.warn('Failed to fetch metrics', err);
        }

        // fallback if trend endpoints not available
        if (!projectsTrend) {
            const base = projectsRes.data.length || 5;
            setProjectsTrend(Array.from({ length: 8 }).map((_, i) => ({ name: String(i), value: Math.max(0, base - (7 - i)) })));
        }
        if (!usersTrend) {
            const baseU = usersRes.data.length || 8;
            setUsersTrend(Array.from({ length: 8 }).map((_, i) => ({ name: String(i), value: Math.max(0, baseU - (7 - i)) })));
        }
    }, [projectsTrend, usersTrend]);

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
                    <div style={{ width: '100%', marginTop: 8, height: 36 }}>
                        <ResponsiveContainer width="100%" height="100%">
                            <LineChart data={useMemo(() => projectsTrend || [], [projectsTrend])}>
                                <Tooltip formatter={(v) => [v, 'Projects']} />
                                <Line type="monotone" dataKey="value" stroke="#2563EB" strokeWidth={2} dot={false} />
                            </LineChart>
                        </ResponsiveContainer>
                    </div>
                </StatCard>
                <StatCard type="button" onClick={() => navigate("/admin/users")}>
                    <StatLabel>Users</StatLabel>
                    <StatNumber>{userCount ?? "…"}</StatNumber>
                    <div style={{ width: '100%', marginTop: 8, height: 36 }}>
                        <ResponsiveContainer width="100%" height="100%">
                            <LineChart data={useMemo(() => usersTrend || [], [usersTrend])}>
                                <Tooltip formatter={(v) => [v, 'Users']} />
                                <Line type="monotone" dataKey="value" stroke="#10B981" strokeWidth={2} dot={false} />
                            </LineChart>
                        </ResponsiveContainer>
                    </div>
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