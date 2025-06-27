FROM alpine:latest

RUN apk add --no-cache openssl

WORKDIR /app

COPY generate-oneground-certificates.sh .

RUN sed -i 's/\r$//' generate-oneground-certificates.sh

RUN chmod +x generate-oneground-certificates.sh

CMD ["./generate-oneground-certificates.sh"]