# YARP Timeouts Playground

The repository demonstrates two use cases of the [Request Timeouts](https://microsoft.github.io/reverse-proxy/articles/timeouts.html) feature of [YARP](https://microsoft.github.io/reverse-proxy/): configuration on a route and on a cluster.

## Cluster

Can be specified by the [`ForwarderRequestConfig.ActivityTimeout`](https://microsoft.github.io/reverse-proxy/articles/http-client-config.html#httprequest) configuration setting on a cluster.

When such configuration is applied, reverse proxy returns `504 Gateway Timeout` in cases of timeouts, check the [integration test](test/Yarp.Timeouts.Playground.Tests/TimeoutsTests.cs#L73).

## Route

Can be specified via the [`Timeout/TimeoutPolicy`](https://microsoft.github.io/reverse-proxy/articles/timeouts.html#configuration) configuration settings on a route.

When such configuration is applied, reverse proxy returns `400 Bad Request` in cases of timeouts, check the [integration test](test/Yarp.Timeouts.Playground.Tests/TimeoutsTests.cs#L105).

## Concern

Should reverse proxy return the same result `504 Gateway Timeout` for both timeouts configurations?