# Build context: repository root (see docker-compose.yml)
# glibc, not Alpine: Rollup's native parser overflows musl's small default thread stack on react-dom.
FROM node:24-bookworm-slim AS build
WORKDIR /repo

# API-first: compile the TypeSpec contract and regenerate the typed client inside the image.
COPY package.json yarn.lock ./
RUN yarn install --frozen-lockfile
COPY api/ api/
RUN yarn tsp compile api

COPY src/Sift.Web/package.json src/Sift.Web/yarn.lock src/Sift.Web/
RUN yarn --cwd src/Sift.Web install --frozen-lockfile
COPY src/Sift.Web/ src/Sift.Web/
RUN yarn --cwd src/Sift.Web generate:api

# Same-origin path that nginx proxies to the API container.
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN yarn --cwd src/Sift.Web build

FROM nginx:1.29-alpine AS runtime
COPY src/Sift.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/src/Sift.Web/dist /usr/share/nginx/html
EXPOSE 80
