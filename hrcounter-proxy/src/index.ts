import hyperate from "./hyperate/handler";

export default {
    async fetch(request, env, ctx): Promise<Response> {
        const url = new URL(request.url);
        const ua = request.headers.get("User-Agent") || "";
        console.log(`Fetch from ${ua} at ${url.pathname}`);

        switch (url.pathname) {
            case "/hyperate":
                //TODO auth and limit max connections per user
                return hyperate.handle(request, env, ctx);
        }

        return new Response("Not Found", { status: 404 });
    },
} satisfies ExportedHandler<Env>;
