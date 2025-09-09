# =============================
# Build Stage
# =============================
FROM node:18-alpine AS build
WORKDIR /app

# Copy package.json & lock file first (better layer caching)
COPY package*.json ./

# Install dependencies
RUN npm install --legacy-peer-deps

# Copy source code and build
COPY . .
RUN npm run build

# =============================
# Runtime Stage (Nginx)
# =============================
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Remove default Nginx static assets
RUN rm -rf ./*

# Copy build artifacts from build stage
COPY --from=build /app/dist ./  

# Expose port 80 for container app ingress
EXPOSE 80

# Start Nginx
CMD ["nginx", "-g", "daemon off;"]
