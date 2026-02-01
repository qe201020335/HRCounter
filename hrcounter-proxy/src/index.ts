import { handleHypeRate } from "./hyperate/handler";
import { verifyUser } from "./auth";

export default {
    async fetch(request, env, ctx): Promise<Response> {
        const url = new URL(request.url);
        const ua = request.headers.get("User-Agent") || "";
        console.log(`Fetch from ${ua} at ${url.pathname}`);

        switch (url.pathname) {
            case "/proxy/hyperate":
                //TODO limit max connections per user
                const auth = await verifyUser(request);
                if (auth !== null) {
                    return auth;
                }
                return handleHypeRate(request, env, ctx);
        }

        return new Response("Not Found", { status: 404 });
    },
} satisfies ExportedHandler<Env>;
