import { useContext } from "react";
import { AuthContext } from "./context";

export function useAuth() {
    return useContext(AuthContext);
}

// we should take the use of useAuth inside the component to take the use of the function created inside the context api 

