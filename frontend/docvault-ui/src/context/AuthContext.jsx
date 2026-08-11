import { useState } from "react";
import { jwtDecode } from "jwt-decode";
import { userApi } from "../api/api";
import { AuthContext } from "./context";

export function AuthProvider({ children }) {
    const [user, setUser] = useState(() => {
        const saved = localStorage.getItem("user");
        return saved ? JSON.parse(saved) : null;
    });

    async function login(email, password) {
        const res = await userApi.post("/auth/login", { email, password });
        const data = res.data;

        const decoded = jwtDecode(data.token);

        // Derive a single "role" for routing from isAdmin flag + projects list.
        // Admin has no project memberships; ProjectHead/User is determined by
        // whether any membership has role "ProjectHead".
        let role = "User";
        if (data.isAdmin) {
            role = "Admin";
        } else if ((data.projects || []).some((p) => p.role === "ProjectHead")) {
            role = "ProjectHead";
        }

        const userData = {
            id: decoded.sub,
            email: data.email,
            fullName: data.fullName,
            isAdmin: data.isAdmin,
            role,                        // derived — used for routing + ProtectedRoute
            projects: data.projects || [], // [{projectId, projectName, role}]
        };

        localStorage.setItem("token", data.token);
        localStorage.setItem("user", JSON.stringify(userData));
        setUser(userData);
        return userData; // Login.jsx reads .role from this for ROLE_HOME redirect
    }

    function logout() {
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        setUser(null);
    }

    return (
        <AuthContext.Provider value={{ user, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
}