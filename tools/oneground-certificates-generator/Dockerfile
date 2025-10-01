FROM alpine:3.22

RUN apk add --no-cache openssl

# Create a non-root user for running the application
RUN addgroup -g 1000 -S certgen && \
    adduser -u 1000 -S certgen -G certgen

WORKDIR /app

ENV DOMAIN=oneground.local

COPY generate-oneground-certificates.sh .

RUN sed -i 's/\r$//' generate-oneground-certificates.sh

RUN chmod +x generate-oneground-certificates.sh

# Change ownership to the non-root user
RUN chown -R certgen:certgen /app

# Switch to non-root user
USER certgen

CMD ["sh", "-c", "./generate-oneground-certificates.sh \"$DOMAIN\""]
