import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '10s', target: 10 },
    { duration: '20s', target: 50 },
    { duration: '30s', target: 100 },
    { duration: '10s', target: 0 },
  ],
};

const BASE_URL = 'http://localhost:5224/api/orders?api-version=1.0';

// Substituir pelo valor copiado do navegador
const AUTH_COOKIE = ".AspNetCore.Identity.Application=CfDJ8BybUKMmYuhClp5aiMnohDrwQEyg1qUrd3y5oqJMkL5NFMoZMG4aNvWy-u5qLV2WOotxYE8W_1rapJw83R5D-U01kCWBYKUnKHdMsmJCd0B2xIt89cuIGwYmJM4GoH2DwNEndwtgaMNA-HUhWDtGAoiaK06P1gIkb07vVsMMaj8yHcIxaYPrHrsMjW7Hvb7uwdNC0WqGX2qOLuX5Vywp3F5_61RFTS8rSzIj4p_KYIGuRF8jnd0Aw5Gr1ljh6r74PjxllW9UQYa3MQgYAMFGEC2-fTaEr9Cnmny2M9LQNLBI4ghI3lBouF1My2MD0lCOd7JataHbAekC8VwERs8XD2e67Mxs4ZFsM11ENx5u-lYThZ4vh1uHGk6Y_RLR7ni_gYHLVyQ1QGxh8I5WGa1HU2ROsNscOqwccQ_nXqRbMRVc_DMd7U4ZbVhRhkcy0y7On-SLDIOCKWgeBLT5cbtxeYGS9ffHq32xi5szNmjx6s-sOdasuBo1e1gZvFXnHP2Lr8qieGya5HsT-pqHhim5wJa6xMLswOfYEoWfa4QCvcF6CtbnH3iSYV1vXvYeq2mcpHYRUH5HKfxj84krcqIiz0tUG986gOpfxdYKUStOFOS0-mUw01GDp4eMuGG8DirCdLfIvvEd5y3IWJUGbbDQQm6_HuEhG5sgKL3GGUc7U3OQijH3K0UktDittbe_fp_8hOuvAO2rfJkolEK437vIxjh_L-h0e2ino6JbFGEMq3yQZm2wDWPDOtHaobxZG-b6tLgz3nom20Bf8JNYgwYEAF9fBrh-WwDnq3DF1kz1VYUlGR8qr6qEPtqKlfFjGjYoZF3aO2pOxBf-QEoAIwUqm3MbdRdlWMo_6nDH3ax0tkTZg3Vs37g1ckklYBnZ3_V1us0RVyWbrm4zi0yPrF1OtL2lwkm7_4uY4HMWlBdYKG5TPSlJNZRc1-ZwVZGFK8xoJo3_a4Scwd3_T0Fkw3cMj1w0mhZMcAMuR-n1leOwbzxMtDjWhiFC4Hpf3300HdZEXzw_zz2pzOW6MYDsZCx6Zcv23Fb4jn-JpAP8bGETJtbhsx7oZSzLmhJJ-Z2SfOcacA";

export default function () {
  let payload = JSON.stringify({
    "userId": "user-123",
    "userName": "test_user",
    "city": "New York",
    "street": "123 Test St",
    "state": "NY",
    "country": "USA",
    "zipCode": "10001",
    "cardNumber": "4111111111111111",
    "cardHolderName": "John Doe",
    "cardExpiration": "12/26",
    "cardSecurityNumber": "123",
    "cardTypeId": 1,
    "buyer": "test_buyer",
    "items": [
      {
        "productId": 1,
        "productName": "Test Product",
        "unitPrice": 100,
        "quantity": 2
      }
    ]
  });

  let headers = {
    'Content-Type': 'application/json',
    'Cookie': AUTH_COOKIE,  // Enviando o cookie de autenticação
  };

  let res = http.post(BASE_URL, payload, { headers });

  console.log(`Response: ${res.status} - ${res.body}`);

  check(res, {
    'is status 200': (r) => r.status === 200,
    'is status 201': (r) => r.status === 201,
  });

  sleep(1);
}
