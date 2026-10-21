// Inlined into the generated api-client.ts by NSwag (see "extensionCode" in nswag.json).

export interface ApiClientConfig {
    /** Returns the current Keycloak access token; called before every request. */
    getAccessToken?: () => string | undefined | Promise<string | undefined>;
}

export class ApiClientBase {
    private readonly config: ApiClientConfig;

    constructor(config: ApiClientConfig) {
        this.config = config;
    }

    protected async transformOptions(options: RequestInit): Promise<RequestInit> {
        const token = await this.config.getAccessToken?.();
        if (!token)
            return options;

        const headers = new Headers(options.headers);
        headers.set("Authorization", `Bearer ${token}`);

        return { ...options, headers };
    }
}
