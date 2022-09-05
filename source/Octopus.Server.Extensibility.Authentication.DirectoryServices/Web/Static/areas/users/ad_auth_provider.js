var providerName = "Active Directory";

function directoryServicesAuthProvider(octopusClient, provider, loginState, onError) {

    this.linkHtml =
        '<a><div class="ds-button"><img src="' + octopusClient.resolve("~/images/directory_services_signin_buttons/microsoft-logo.svg") + '"><div>Sign in with a domain account</div></div></a>';

    this.signIn = function () {
        console.log("Signing in using " + providerName + " provider");

        var authUri = octopusClient.resolve(provider.Links.Authenticate);
        authUri += "?state=" + encodeURIComponent(JSON.stringify(loginState));

        window.location.href = authUri;
    };

    return {
        Name: providerName,
        LinkHtml: this.linkHtml,
        SignIn: this.signIn
    };
}

console.log("Registering " + providerName + " auth provider");
window.Octopus.registerExtension(providerName, "auth_provider", directoryServicesAuthProvider);