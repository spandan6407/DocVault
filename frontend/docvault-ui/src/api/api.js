import axios from "axios";

const USER_API = "http://localhost:5261/api";
const DOC_API = "http://localhost:5078/api";

function authHeader() {
    const token = localStorage.getItem("token");

    return token
        ? { Authorization: `Bearer ${token}` }
        : {};
}


// this is the creation of the axios instances from the base urls 
export const userApi = axios.create({
    baseURL: USER_API,
});

export const docApi = axios.create({
    baseURL: DOC_API,
});

// Attach JWT ... attachment of jwt tokens inside the axios instancs....userApi 
userApi.interceptors.request.use(
    (config) => {
        config.headers = {
            ...config.headers,
            ...authHeader(),
        };

        return config;
    },
    (error) => Promise.reject(error)
);

// Attach JWT to Document API axios instances 
docApi.interceptors.request.use(
    (config) => {
        config.headers = {
            ...config.headers,
            ...authHeader(),
        };

        return config;
    },
    (error) => Promise.reject(error)
);



// console error message if  any of the api fails and we are doing this with the help of axios interceptors 
// handling of the api errros and we should take the use of the interceptors for showing the error logs 
userApi.interceptors.response.use(
    (response) => response,
    (error) => {
        console.error("User API Error:", error.response?.data || error.message);
        return Promise.reject(error);
    }
);

docApi.interceptors.response.use(
    (response) => response,
    (error) => {
        console.error("Document API Error:", error.response?.data || error.message);
        return Promise.reject(error);
    }
);