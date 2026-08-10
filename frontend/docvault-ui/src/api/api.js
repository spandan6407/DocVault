import axios from "axios";

const USER_API = "http://localhost:5261/api";
const DOC_API = "http://localhost:5078/api";

function authHeader() {
    const token = localStorage.getItem("token");
    return token ? { Authorization: `Bearer ${token}` } : {};
}

export const userApi = axios.create({ baseURL: USER_API });
export const docApi = axios.create({ baseURL: DOC_API });

userApi.interceptors.request.use((config) => {
    config.headers = { ...config.headers, ...authHeader() };
    return config;
});

docApi.interceptors.request.use((config) => {
    config.headers = { ...config.headers, ...authHeader() };
    return config;
});