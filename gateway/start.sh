#!/bin/sh
set -e

# Extract DNS resolver from /etc/resolv.conf for nginx dynamic upstream resolution
RAW_RESOLVER=$(grep -m1 '^nameserver' /etc/resolv.conf | awk '{print $2}')
if [ -z "$RAW_RESOLVER" ]; then
    RAW_RESOLVER="8.8.8.8"
fi

# Wrap IPv6 addresses in brackets for nginx resolver directive
case "$RAW_RESOLVER" in
    *:*) DNS_RESOLVER="[$RAW_RESOLVER]" ;;
    *)   DNS_RESOLVER="$RAW_RESOLVER" ;;
esac
export DNS_RESOLVER

# Substitute the resolver into nginx.conf
envsubst '${DNS_RESOLVER}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

echo "Starting nginx with DNS resolver: $DNS_RESOLVER"
exec nginx -g 'daemon off;'
