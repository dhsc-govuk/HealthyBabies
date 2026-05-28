#!/bin/sh
set -e

# Default to / if BASE_PATH is not set
BASE_PATH=${BASE_PATH:-/}

# Remove trailing slash for nginx location matching (except for root)
if [ "$BASE_PATH" != "/" ]; then
    BASE_PATH_TRIMMED=$(echo "$BASE_PATH" | sed 's:/$::')
else
    BASE_PATH_TRIMMED="/"
fi

# Replace BASE_PATH placeholder in nginx config
sed "s|BASE_PATH|${BASE_PATH_TRIMMED}|g" /etc/nginx/conf.d/default.conf.template > /etc/nginx/conf.d/default.conf

echo "Configured nginx with BASE_PATH: ${BASE_PATH_TRIMMED}"
cat /etc/nginx/conf.d/default.conf

# Execute the CMD
exec "$@"
