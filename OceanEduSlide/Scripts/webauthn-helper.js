//
// WebAuthn Helper Functions - FULL AUTO
//

// Kiểm tra trình duyệt có hỗ trợ WebAuthn / Passkey không
function isPasskeySupported() {
    return (window.PublicKeyCredential !== undefined &&
        typeof PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable === "function");
}

// Kiểm tra thiết bị có hỗ trợ FaceID/TouchID Passkey platform authenticator
async function isPlatformAuthenticatorAvailable() {
    if (!isPasskeySupported()) {
        return false;
    }

    try {
        return await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
    } catch (err) {
        console.error("Check platform authenticator error:", err);
        return false;
    }
}

// Base64 string ➔ Uint8Array
function bufferDecode(value) {
    return Uint8Array.from(atob(value), c => c.charCodeAt(0));
}

// Uint8Array ➔ Base64 string
function bufferEncode(value) {
    return btoa(String.fromCharCode.apply(null, new Uint8Array(value)));
}

// Base64URL encode (sử dụng cho WebAuthn chuẩn)
function bufferEncodeURL(value) {
    return bufferEncode(value)
        .replace(/\+/g, "-")
        .replace(/\//g, "_")
        .replace(/=/g, "");
}

// Base64URL decode
function bufferDecodeURL(value) {
    value = value.replace(/-/g, "+").replace(/_/g, "/");
    while (value.length % 4 !== 0) {
        value += "=";
    }
    return bufferDecode(value);
}

// ✅ Preformat cho MakeCredential Request
function preformatMakeCredReq(makeCredReq) {
    try {
        if (Array.isArray(makeCredReq.challenge)) {
            makeCredReq.challenge = new Uint8Array(makeCredReq.challenge);
        } else {
            makeCredReq.challenge = bufferDecodeURL(makeCredReq.challenge);
        }

        if (Array.isArray(makeCredReq.user.id)) {
            makeCredReq.user.id = new Uint8Array(makeCredReq.user.id);
        } else {
            makeCredReq.user.id = bufferDecodeURL(makeCredReq.user.id);
        }

        if (makeCredReq.excludeCredentials) {
            makeCredReq.excludeCredentials = makeCredReq.excludeCredentials.map(cred => {
                if (Array.isArray(cred.id)) {
                    cred.id = new Uint8Array(cred.id);
                } else {
                    cred.id = bufferDecodeURL(cred.id);
                }
                return cred;
            });
        }
    } catch (err) {
        console.error("Error in preformatMakeCredReq:", err);
        throw err;
    }
    return makeCredReq;
}

// ✅ Preformat cho GetAssertion Request
function preformatGetAssertReq(getAssert) {
    try {
        if (Array.isArray(getAssert.challenge)) {
            getAssert.challenge = new Uint8Array(getAssert.challenge);
        } else {
            getAssert.challenge = bufferDecodeURL(getAssert.challenge);
        }

        if (getAssert.allowCredentials) {
            getAssert.allowCredentials = getAssert.allowCredentials.map(cred => {
                if (Array.isArray(cred.id)) {
                    cred.id = new Uint8Array(cred.id);
                } else {
                    cred.id = bufferDecodeURL(cred.id);
                }
                return cred;
            });
        }
    } catch (err) {
        console.error("Error in preformatGetAssertReq:", err);
        throw err;
    }
    return getAssert;
}

// ✅ Format Credential Response sau Create
function formatMakeCredResp(cred) {
    return {
        id: cred.id,
        rawId: bufferEncodeURL(cred.rawId),
        type: cred.type,
        response: {
            attestationObject: bufferEncodeURL(cred.response.attestationObject),
            clientDataJSON: bufferEncodeURL(cred.response.clientDataJSON)
        }
    };
}

// ✅ Format Credential Response sau Assertion
function formatGetAssertResp(assertion) {
    return {
        id: assertion.id,
        rawId: bufferEncodeURL(assertion.rawId),
        type: assertion.type,
        response: {
            authenticatorData: bufferEncodeURL(assertion.response.authenticatorData),
            clientDataJSON: bufferEncodeURL(assertion.response.clientDataJSON),
            signature: bufferEncodeURL(assertion.response.signature),
            userHandle: assertion.response.userHandle ? bufferEncodeURL(assertion.response.userHandle) : null
        }
    };
}

// ✅ Debug Logger (auto in ra publicKey options)
function debugPublicKeyOptions(publicKey) {
    console.group("PublicKeyCredentialOptions Debug:");
    console.log("Challenge (Uint8Array):", publicKey.challenge);
    if (publicKey.user) {
        console.log("User ID (Uint8Array):", publicKey.user.id);
    }
    if (publicKey.allowCredentials) {
        console.log("AllowCredentials:", publicKey.allowCredentials.map(c => c.id));
    }
    console.groupEnd();
}
