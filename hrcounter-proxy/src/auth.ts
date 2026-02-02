const USER_PLATFORM_HEADER = "X-User-Platform";
const USER_ID_HEADER = "X-User-ID";
const USER_TOKEN_HEADER = "X-User-Token";

export const verifyUser = async (request: Request) => {
    const platform = request.headers.get(USER_PLATFORM_HEADER);
    const userId = request.headers.get(USER_ID_HEADER);
    const userToken = request.headers.get(USER_TOKEN_HEADER);

    let steamId: string | null = null;
    let oculusId: string | null = null;

    if (!platform || !userId || !userToken) {
        return new Response("Invalid authorization", { status: 400 });
    }

    if (platform === "steam") {
        steamId = userId;
    } else if (platform === "oculus") {
        oculusId = userId;
    } else {
        return new Response("Invalid authorization", { status: 400 });
    }


    const body = `{"steamId": "${steamId}","oculusId": "${oculusId}","proof": "${userToken}"}`;
    const response = await fetch("https://api.beatsaver.com/users/verify", {
        body: body,
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        }
    });

    if (!response.ok) {
        console.warn("User verification request failed with status", response.status);
        return new Response(response.body, { status: response.status });
    }

    try {
        // @ts-ignore
        if ((await response.json())["success"] === true) {
            return null;
        }

        return new Response("Unauthorized", { status: 401 });
    } catch (error) {
        console.error("Failed to parse user verification response:", error);
        return new Response("Internal Server Error", { status: 500 });
    }
};
