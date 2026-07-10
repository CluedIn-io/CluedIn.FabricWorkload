# =============================
# Build Stage
# =============================
FROM node:18-alpine AS build
WORKDIR /app

# Increase Node memory for install & build
ENV NODE_OPTIONS="--max_old_space_size=6144"

# Copy package.json & lock file
COPY package*.json ./

# Install deps
RUN npm install

# Copy source
COPY . .

# Build
RUN npm run build:prod2

# =============================
# Runtime Stage
# =============================
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*

COPY --from=build /app/tools/dist ./

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
