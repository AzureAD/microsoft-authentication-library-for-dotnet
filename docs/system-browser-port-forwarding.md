# System browser authentication in Linux containers

If the browser runs on the host and an IPv4-forwarded callback cannot reach the
container's IPv6 `localhost` listener, set this environment variable in the
container before starting the application or authentication tool:

```bash
export MSAL_ALLOW_SYSTEM_BROWSER_PORT_FORWARDING=true
```

Only `true` (case-insensitive) or `1` enables the behavior. An unset, empty, or
any other value leaves it disabled. The value is read when each listener is
constructed and does not change an existing listener. Child processes can
inherit the setting, so scope it to the container or tool that needs it.

This setting affects only the Linux system browser listener.
It adds an IPv4 wildcard listener, retaining the `localhost`
listener when it resolves to IPv6 first. Keep the registered redirect URI as
`http://localhost:<port>` and configure forwarding for that port separately.
The setting does not create a port mapping or launch a browser on the host.

**Security:** Enable this only in an isolated container. The listener becomes
reachable on **all container IPv4 interfaces**, not just loopback. Restrict the
published host port to loopback and restrict access from other containers.
Do not enable this in a host-networked container or on an untrusted network.
