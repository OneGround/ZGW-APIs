FROM alpine:3.22

RUN apk add --no-cache openssl

WORKDIR /app

ENV DOMAIN=oneground.local

COPY generate-oneground-certificates.sh .

RUN sed -i 's/\r$//' generate-oneground-certificates.sh

RUN chmod +x generate-oneground-certificates.sh

CMD "./generate-oneground-certificates.sh" $DOMAIN