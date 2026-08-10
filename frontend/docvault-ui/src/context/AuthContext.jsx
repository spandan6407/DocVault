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
        const userId = decoded.sub;

        const userData = {
            id: userId,
            email: data.email,
            fullName: data.fullName,
            role: data.role,
            projectId: data.projectId,
        };

        localStorage.setItem("token", data.token);
        localStorage.setItem("user", JSON.stringify(userData));
        setUser(userData);
        return data;
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