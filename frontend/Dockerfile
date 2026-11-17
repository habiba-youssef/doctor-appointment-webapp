
# Stage 1: Build Angular
FROM node:22-alpine AS build

WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm ci

# Copy Angular source files
COPY . .

# Create production build
RUN npm run build


# Stage 2: Serve Angular with Nginx
FROM nginx:alpine

# Copy the compiled Angular application
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Copy web server configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
