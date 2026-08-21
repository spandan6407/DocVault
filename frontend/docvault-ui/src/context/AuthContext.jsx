import { useState } from "react";
import { jwtDecode } from "jwt-decode";
import { userApi } from "../api/api";
import { AuthContext } from "./context";
// import of the authcontext variable



// authprovider is the function that is used to provide the data to the component
// it provide the data to the child 

export function AuthProvider({ children }) {
    const [user, setUser] = useState(() => {
        const saved = localStorage.getItem("user");
        return saved ? JSON.parse(saved) : null;
    });

    async function login(email, password) {
        const res = await userApi.post("/auth/login", { email, password });
        const data = res.data;

        const decoded = jwtDecode(data.token);


        let role = "User";
        if (data.isAdmin) {
            role = "Admin";
        } else if ((data.projects || []).some((p) => p.role === "ProjectHead")) {
            role = "ProjectHead";
        }
        // else the role will stay as the user ...

        const userData = {
            id: decoded.sub,
            email: data.email,
            fullName: data.fullName,
            isAdmin: data.isAdmin,
            role,                        
            projects: data.projects || [], // [{projectId, projectName, role}]
        };

        localStorage.setItem("token", data.token);
        localStorage.setItem("user", JSON.stringify(userData));
        setUser(userData);
        return userData; 
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