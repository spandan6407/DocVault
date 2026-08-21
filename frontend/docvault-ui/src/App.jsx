import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { ThemeProvider } from "styled-components";
import { theme, GlobalStyle } from "./styles/theme";
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";

// Login
const Login = lazy(() => import("./pages/Login"));

// Admin pages
const AdminDashboard = lazy(() => import("./pages/AdminDashboard"));
const ProjectsPage = lazy(() => import("./pages/admin/ProjectsPage"));
const UsersPage = lazy(() => import("./pages/admin/UsersPage"));
const CreateProjectPage = lazy(
    () => import("./pages/admin/CreateProjectPage")
);
const CreateUserPage = lazy(
    () => import("./pages/admin/CreateUserPage")
);
const RequestsPage = lazy(
    () => import("./pages/admin/RequestsPage")
);

// User / Project pages...
const Dashboard = lazy(() => import("./pages/user/Dashboard"));
const ProjectDocumentsPage = lazy(
    () => import("./pages/user/ProjectDocumentsPage")
);
const MembersPage = lazy(
    () => import("./pages/user/MembersPage")
);
const UploadPage = lazy(
    () => import("./pages/user/UploadPage")
);
const WritePage = lazy(
    () => import("./pages/user/WritePage")
);
const ChangeProjectPage = lazy(
    () => import("./pages/user/ChangeProjectPage")
);

const AdminOnly = ({ children }) => (
    <ProtectedRoute allowedRoles={["Admin"]}>
        {children}
    </ProtectedRoute>
);

export default function App() {
    return (
        <ThemeProvider theme={theme}>
            <GlobalStyle />

            <BrowserRouter>
                <AuthProvider>
                    <Suspense fallback={<div>Loading...</div>}>
                        <Routes>

                            {/* LOGIN */}

                            <Route
                                path="/login"
                                element={<Login />}
                            />

                            {/* ADMIN */}

                            <Route
                                path="/admin"
                                element={
                                    <AdminOnly>
                                        <AdminDashboard />
                                    </AdminOnly>
                                }
                            />

                            <Route
                                path="/admin/projects"
                                element={
                                    <AdminOnly>
                                        <ProjectsPage />
                                    </AdminOnly>
                                }
                            />

                            <Route
                                path="/admin/projects/new"
                                element={
                                    <AdminOnly>
                                        <CreateProjectPage />
                                    </AdminOnly>
                                }
                            />

                            <Route
                                path="/admin/users"
                                element={
                                    <AdminOnly>
                                        <UsersPage />
                                    </AdminOnly>
                                }
                            />

                            <Route
                                path="/admin/users/new"
                                element={
                                    <AdminOnly>
                                        <CreateUserPage />
                                    </AdminOnly>
                                }
                            />

                            <Route
                                path="/admin/requests"
                                element={
                                    <AdminOnly>
                                        <RequestsPage />
                                    </AdminOnly>
                                }
                            />

                            {/* USER / PROJECT */}

                            <Route
                                path="/projects"
                                element={
                                    <ProtectedRoute>
                                        <Dashboard />
                                    </ProtectedRoute>
                                }
                            />

                            <Route
                                path="/projects/:projectId/documents"
                                element={
                                    <ProtectedRoute>
                                        <ProjectDocumentsPage />
                                    </ProtectedRoute>
                                }
                            />

                            <Route
                                path="/projects/:projectId/members"
                                element={
                                    <ProtectedRoute>
                                        <MembersPage />
                                    </ProtectedRoute>
                                }
                            />

                            <Route
                                path="/projects/:projectId/upload"
                                element={
                                    <ProtectedRoute>
                                        <UploadPage />
                                    </ProtectedRoute>
                                }
                            />

                            <Route
                                path="/projects/:projectId/write"
                                element={
                                    <ProtectedRoute>
                                        <WritePage />
                                    </ProtectedRoute>
                                }
                            />

                            <Route
                                path="/projects/change"
                                element={
                                    <ProtectedRoute>
                                        <ChangeProjectPage />
                                    </ProtectedRoute>
                                }
                            />

                            <Route
                                path="*"
                                element={<Navigate to="/login" replace />}
                            />

                        </Routes>
                    </Suspense>
                </AuthProvider>
            </BrowserRouter>
        </ThemeProvider>
    );
}