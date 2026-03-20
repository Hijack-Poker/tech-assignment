FROM nginx:alpine

# Remove default config
RUN rm /etc/nginx/conf.d/default.conf

# Copy nginx config as template (start.sh will substitute DNS_RESOLVER)
COPY nginx.conf /etc/nginx/nginx.conf.template

# Copy startup script
COPY start.sh /start.sh
RUN chmod +x /start.sh

# Copy WebGL build (run copy-webgl.sh before building)
COPY webgl/ /usr/share/nginx/html/

EXPOSE 8080

CMD ["/start.sh"]
