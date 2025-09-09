# =============================
# Build Stage
# =============================
FROM node:18-alpine AS build
WORKDIR /app

# Copy package.json & lock file
COPY package*.json ./

# Install deps
RUN npm install --legacy-peer-deps

# Copy source
COPY . .

# Copy env file (needed for build)
COPY .env.prod ./

# Increase Node memory for Webpack
ENV NODE_OPTIONS="--max_old_space_size=4096"

# Build
RUN npm run build:prod

# =============================
# Runtime Stage
# =============================
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*

COPY --from=build /app/dist ./

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
