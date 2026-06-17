declare module '*/config.json' {
  interface Config {
    api_url: string;
    ad_tenant_id: string;
    ad_authority: string;
    ad_client_id: string;
    ad_scope: string;
    ad_redirect_uri: string;
    ad_domain_hint: string;
    app_insights_connection_string: string;
  }

  const config: Config;
  export default config;
}
