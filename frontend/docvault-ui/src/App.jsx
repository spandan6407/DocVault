import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { ThemeProvider } from "styled-components";
import { theme, GlobalStyle } from "./styles/theme";
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import Login from "./pages/Login";
import AdminDashboard from "./pages/AdminDashboard";
import ProjectsPage from "./pages/admin/ProjectsPage";
import UsersPage from "./pages/admin/UsersPage";
import CreateProjectPage from "./pages/admin/CreateProjectPage";
import CreateUserPage from "./pages/admin/CreateUserPage";
import RequestsPage from "./pages/admin/RequestsPage";
import ProjectHeadDashboard from "./pages/ProjectHeadDashboard";
import UserDashboard from "./pages/UserDashboard";

const AdminOnly = ({ children }) => <ProtectedRoute allowedRoles={["Admin"]}>{children}</ProtectedRoute>;

export default function App() {
    return (
        <ThemeProvider theme={theme}>
            <GlobalStyle />
            <BrowserRouter>
                <AuthProvider>
                    <Routes>
                        <Route path="/login" element={<Login />} />
                        <Route path="/admin" element={<AdminOnly><AdminDashboard /></AdminOnly>} />
                        <Route path="/admin/projects" element={<AdminOnly><ProjectsPage /></AdminOnly>} />
                        <Route path="/admin/projects/new" element={<AdminOnly><CreateProjectPage /></AdminOnly>} />
                        <Route path="/admin/users" element={<AdminOnly><UsersPage /></AdminOnly>} />
                        <Route path="/admin/users/new" element={<AdminOnly><CreateUserPage /></AdminOnly>} />
                        <Route path="/admin/requests" element={<AdminOnly><RequestsPage /></AdminOnly>} />
                        <Route
                            path="/project-head"
                            element={
                                <ProtectedRoute allowedRoles={["ProjectHead"]}>
                                    <ProjectHeadDashboard />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/user"
                            element={
                                <ProtectedRoute allowedRoles={["User"]}>
                                    <UserDashboard />
                                </ProtectedRoute>
                            }
                        />
                        <Route path="*" element={<Navigate to="/login" replace />} />
                    </Routes>
                </AuthProvider>
            </BrowserRouter>
        </ThemeProvider>
    );
}