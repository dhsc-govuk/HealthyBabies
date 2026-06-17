export function validateEmailAddress(emailAddress: string): boolean {
  // eslint-disable-next-line no-control-regex
  const re = /^(?!.*[\x00-\x1F\x7F])^(?!.*\.{2,})^[a-zA-Z0-9][a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]{0,63}[^<>()[\]\\.,;:@"]@([a-zA-Z0-9][a-zA-Z0-9-]{0,62}[a-zA-Z0-9]\.)+[a-zA-Z]{2,}$/;
  return !emailAddress || re.test(emailAddress);
}
