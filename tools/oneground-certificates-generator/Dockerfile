FROM alpine:3.22

RUN apk add --no-cache openssl

RUN addgroup -g 1000 -S certgen && \
    adduser -u 1000 -S certgen -G certgen

WORKDIR /app

ENV DOMAIN=oneground.local

COPY generate-oneground-certificates.sh .

RUN sed -i 's/\r$//' generate-oneground-certificates.sh

RUN chmod +x generate-oneground-certificates.sh

RUN chown -R certgen:certgen /app

USER certgen

CMD ["sh", "-c", "./generate-oneground-certificates.sh \"$DOMAIN\""]
