FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY webgl/ /usr/share/nginx/html/
RUN chmod -R 644 /usr/share/nginx/html/Build/*.br

EXPOSE 8080
