FROM node:22-alpine AS build
WORKDIR /app

ARG VITE_API_URL
ARG VITE_B2C_CLIENT_ID
ARG VITE_B2C_AUTHORITY
ARG VITE_B2C_KNOWN_AUTHORITIES
ARG VITE_B2C_REDIRECT_URI
ARG VITE_BASE_PATH=/

COPY package*.json ./
RUN npm ci

COPY . .
ENV VITE_BASE_PATH=${VITE_BASE_PATH}
RUN npm run build

FROM mcr.microsoft.com/mirror/docker/library/nginx:1.25 AS runner

ARG VITE_BASE_PATH=/

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf.template
COPY docker-entrypoint.sh /docker-entrypoint.sh

RUN chmod +x /docker-entrypoint.sh

ENV BASE_PATH=${VITE_BASE_PATH}

EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
