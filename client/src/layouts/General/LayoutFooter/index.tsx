import React from 'react';
import { Box, Grid } from '@mui/material';
import {
  FooterRoot,
  FooterContainer,
  FooterSection,
  FooterHeading,
  FooterList,
  FooterListItem,
  FooterLink,
  FooterDivider,
  FooterMeta,
  FooterMetaLinks,
  FooterMetaLink,
  FooterLicence,
  FooterLicenceText,
  CopyrightLogo,
  CopyrightText,
  OglLogoWrapper,
} from './styles';
import CrestLogo from '../../../components/Logos/CrestLogo';

const topicsLinks = [
  { label: 'Benefits', href: 'https://www.gov.uk/browse/benefits' },
  { label: 'Births, death, marriages and care', href: 'https://www.gov.uk/browse/births-deaths-marriages' },
  { label: 'Business and self-employed', href: 'https://www.gov.uk/browse/business' },
  { label: 'Childcare and parenting', href: 'https://www.gov.uk/browse/childcare-parenting' },
  { label: 'Citizenship and living in the UK', href: 'https://www.gov.uk/browse/citizenship' },
  { label: 'Crime, justice and the law', href: 'https://www.gov.uk/browse/justice' },
  { label: 'Disabled people', href: 'https://www.gov.uk/browse/disabilities' },
  { label: 'Driving and transport', href: 'https://www.gov.uk/browse/driving' },
  { label: 'Education and learning', href: 'https://www.gov.uk/browse/education' },
  { label: 'Employing people', href: 'https://www.gov.uk/browse/employing-people' },
  { label: 'Environment and countryside', href: 'https://www.gov.uk/browse/environment-countryside' },
  { label: 'Housing and local services', href: 'https://www.gov.uk/browse/housing-local-services' },
  { label: 'Money and tax', href: 'https://www.gov.uk/browse/tax' },
  { label: 'Passports, travel and living abroad', href: 'https://www.gov.uk/browse/abroad' },
  { label: 'Visas and immigration', href: 'https://www.gov.uk/browse/visas-immigration' },
  { label: 'Working, jobs and pensions', href: 'https://www.gov.uk/browse/working' },
];

const governmentActivityLinks = [
  { label: 'Departments', href: 'https://www.gov.uk/government/organisations' },
  { label: 'News', href: 'https://www.gov.uk/search/news-and-communications' },
  { label: 'Guidance and regulation', href: 'https://www.gov.uk/search/guidance-and-regulation' },
  { label: 'Research and statistics', href: 'https://www.gov.uk/search/research-and-statistics' },
  { label: 'Policy papers and consultations', href: 'https://www.gov.uk/search/policy-papers-and-consultations' },
  { label: 'Transparency', href: 'https://www.gov.uk/search/transparency-and-freedom-of-information-releases' },
  { label: 'How government works', href: 'https://www.gov.uk/government/how-government-works' },
  { label: 'Get involved', href: 'https://www.gov.uk/government/get-involved' },
];

const metaLinks = [
  { label: 'Help', href: 'https://www.gov.uk/help' },
  { label: 'Privacy', href: 'https://www.gov.uk/help/privacy-notice' },
  { label: 'Cookies', href: 'https://www.gov.uk/help/cookies' },
  { label: 'Accessibility statement', href: 'https://www.gov.uk/help/accessibility-statement' },
  { label: 'Contact', href: 'https://www.gov.uk/contact' },
  { label: 'Terms and conditions', href: 'https://www.gov.uk/help/terms-conditions' },
];

const metaLinksSecondRow = [
  { label: 'Rhestr o Wasanaethau Cymraeg', href: 'https://www.gov.uk/cymraeg' },
  { label: 'Government Digital Service', href: 'https://www.gov.uk/government/organisations/government-digital-service' },
];

const LayoutFooter = (): React.ReactElement => {
  return (
    <FooterRoot component="footer" role="contentinfo">
      <FooterContainer>
        <Grid container spacing={4}>
          {/* Topics Column */}
          <Grid item xs={12} md={8}>
            <FooterSection>
              <FooterHeading>Topics</FooterHeading>
              <FooterList>
                {topicsLinks.map((link) => (
                  <FooterListItem key={link.label}>
                    <FooterLink href={link.href} target="_blank" rel="noopener noreferrer">
                      {link.label}
                    </FooterLink>
                  </FooterListItem>
                ))}
              </FooterList>
            </FooterSection>
          </Grid>

          {/* Government Activity Column */}
          <Grid item xs={12} md={4}>
            <FooterSection>
              <FooterHeading>Government activity</FooterHeading>
              <FooterList style={{ columnCount: 1 }}>
                {governmentActivityLinks.map((link) => (
                  <FooterListItem key={link.label}>
                    <FooterLink href={link.href} target="_blank" rel="noopener noreferrer">
                      {link.label}
                    </FooterLink>
                  </FooterListItem>
                ))}
              </FooterList>
            </FooterSection>
          </Grid>
        </Grid>

        {/* Divider after Topics/Government activity */}
        <FooterDivider />

        {/* OGL Logo */}
        <Box sx={{ mb: 3 }}>
          <svg
            width="32"
            height="31"
            viewBox="0 0 32 31"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden="true"
            focusable="false"
          >
            <path
              fillRule="evenodd"
              clipRule="evenodd"
              d="M20.1663 9.47976C19.7762 8.53178 20.2263 7.43253 21.1766 7.04931C22.1069 6.66609 23.1872 7.1199 23.5774 8.06788C23.9675 9.01586 23.5173 10.0949 22.5871 10.4882C21.6368 10.8816 20.5564 10.4277 20.1763 9.47976H20.1663ZM15.3048 13.7658C14.3545 14.1591 13.9143 15.2584 14.2945 16.1963C14.6846 17.1442 15.7549 17.5981 16.7052 17.2048C17.6355 16.8215 18.0857 15.7324 17.6955 14.7844C17.3054 13.8364 16.2251 13.3725 15.2948 13.7658H15.3048ZM27.5286 13.4532C28.4589 13.07 28.909 11.9808 28.5189 11.0328C28.1288 10.0848 27.0485 9.62095 26.1182 10.0143C25.1679 10.4076 24.7277 11.5068 25.1078 12.4447C25.498 13.3927 26.5683 13.8465 27.5186 13.4431L27.5286 13.4532ZM28.3889 17.356C28.779 18.304 29.8493 18.7578 30.7996 18.3544C31.7299 17.9712 32.1801 16.882 31.7899 15.9341C31.3998 14.9861 30.3195 14.5222 29.3892 14.9155C28.4389 15.3088 27.9987 16.4081 28.3789 17.3459L28.3889 17.356ZM15.0247 4.71971C15.0947 4.81047 15.1747 4.89115 15.2648 4.96175L13.9244 9.03603V9.05619C13.8643 9.25789 13.8243 9.47976 13.8243 9.70162C13.8243 10.8009 14.6346 11.7186 15.6849 11.8699H15.7349C15.8249 11.88 15.915 11.89 16.005 11.89C16.095 11.89 16.1851 11.89 16.2751 11.8699H16.3251C17.3754 11.7085 18.1857 10.8009 18.1857 9.70162C18.1857 9.47976 18.1557 9.25789 18.0857 9.05619V9.03603L16.7452 4.96175C16.8353 4.89115 16.9153 4.81047 16.9853 4.71971C17.0553 4.62895 19.316 5.95006 19.316 5.95006V2.48087L16.9853 3.22715C16.9253 3.13639 16.8453 3.05571 16.7552 2.98512C16.6652 2.91452 17.6955 0 17.6955 0H14.3345L15.2748 2.97503C15.1947 3.04562 15.1147 3.1263 15.0447 3.21707C14.9747 3.30783 12.714 2.48087 12.714 2.48087V5.95006L15.0447 4.70962L15.0247 4.71971ZM9.39294 10.4882C10.3432 10.8816 11.4236 10.4277 11.8037 9.47976C12.1938 8.53178 11.7437 7.43253 10.7934 7.04931C9.86308 6.66609 8.78274 7.1199 8.39262 8.06788C8.0025 9.01586 8.45264 10.0949 9.38293 10.4882H9.39294ZM4.45139 13.4633C5.40169 13.8566 6.48203 13.4028 6.86214 12.4649C7.25227 11.5169 6.80213 10.4176 5.85183 10.0344C4.92154 9.6512 3.8412 10.105 3.45108 11.053C3.06096 12.001 3.5111 13.08 4.44139 13.4734L4.45139 13.4633ZM1.18037 18.3746C2.13067 18.7679 3.211 18.3141 3.59112 17.3762C3.98124 16.4282 3.5311 15.329 2.58081 14.9457C1.65052 14.5625 0.570178 15.0163 0.180056 15.9643C-0.210066 16.9123 0.240075 17.9914 1.17037 18.3847L1.18037 18.3746ZM29.5092 20.4722C29.7593 21.753 29.7993 22.348 29.5192 23.175C29.1091 22.7716 28.719 22.0152 28.4089 20.8756L27.1985 24.9499C27.9387 24.4356 28.5089 24.1028 29.1591 24.0927C28.0088 26.6038 26.5583 27.2493 25.628 27.0677C24.4877 26.856 23.9575 25.8273 24.1375 24.96C24.3976 23.7297 25.658 23.4069 26.2382 24.839C27.3585 22.5296 25.458 21.8135 24.2376 22.4993C26.1182 20.6033 26.3282 18.9293 24.8178 16.8921C22.7071 18.5259 22.6771 20.1394 23.6274 22.4085C22.397 20.9866 20.4664 21.753 21.1666 24.0524C22.0569 22.6607 23.2373 23.538 23.0472 24.8592C22.8872 26.0088 21.3867 26.9366 19.5061 26.7753C16.8153 26.5332 16.6552 24.6575 16.5852 23.1145C17.2454 22.9935 18.4358 23.6086 19.4561 25.0508L19.8262 20.7344C18.7259 21.8942 17.7155 22.1161 16.6052 22.1463C16.9753 20.9765 18.6858 19.0704 18.6858 19.0704H13.3342C13.3342 19.0704 15.0347 20.9866 15.4148 22.1463C14.3045 22.106 13.2942 21.8942 12.1938 20.7344L12.5639 25.0508C13.5842 23.6187 14.7646 22.9935 15.4348 23.1145C15.3648 24.6675 15.2048 26.5332 12.5139 26.7753C10.6333 26.9366 9.13285 26.0088 8.9728 24.8592C8.78274 23.538 9.96311 22.6607 10.8534 24.0524C11.5436 21.753 9.62301 20.9866 8.39262 22.4085C9.34292 20.1394 9.32291 18.5259 7.20225 16.8921C5.68178 18.9293 5.90184 20.6033 7.78243 22.4993C6.56205 21.8135 4.65145 22.5396 5.78181 24.839C6.37199 23.4069 7.62238 23.7297 7.88246 24.96C8.06252 25.8273 7.53235 26.856 6.392 27.0677C5.46171 27.2392 4.01125 26.5937 2.86089 24.0927C3.5111 24.1129 4.08128 24.4356 4.82151 24.9499L3.61113 20.8756C3.30103 22.0152 2.91091 22.7716 2.50078 23.175C2.22069 22.348 2.26071 21.753 2.50078 20.4722L0 21.3698C1.33042 23.1952 2.62082 25.7668 3.67115 30.2545C7.40231 29.7201 11.5836 29.4175 15.995 29.4175C20.4064 29.4175 24.5977 29.7201 28.3289 30.2545C29.3892 25.7668 30.6796 23.1952 32 21.3698L29.4992 20.4722H29.5092Z"
              fill="#0B0C0C"
            />
          </svg>
        </Box>

        {/* Footer Meta */}
        <FooterMeta>
          <Box sx={{ flex: 1 }}>
            <FooterMetaLinks>
              {metaLinks.map((link) => (
                <FooterMetaLink key={link.label} href={link.href} target="_blank" rel="noopener noreferrer">
                  {link.label}
                </FooterMetaLink>
              ))}
            </FooterMetaLinks>
            <FooterMetaLinks sx={{ mt: 1 }}>
              {metaLinksSecondRow.map((link) => (
                <FooterMetaLink key={link.label} href={link.href} target="_blank" rel="noopener noreferrer">
                  {link.label}
                </FooterMetaLink>
              ))}
            </FooterMetaLinks>
            <FooterLicence>
              <OglLogoWrapper>
                <svg
                  aria-hidden="true"
                  focusable="false"
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 483.2 195.7"
                  height="17"
                  width="41"
                >
                  <path
                    fill="currentColor"
                    d="M421.5 142.8V.1l-50.7 32.3v161.1h112.4v-50.7zm-122.3-9.6A47.12 47.12 0 0 1 221 97.8c0-26 21.1-47.1 47.1-47.1 16.7 0 31.4 8.7 39.7 21.8l42.7-27.2A97.63 97.63 0 0 0 268.1 0c-36.5 0-68.3 20.1-85.1 49.7A98 98 0 0 0 97.8 0C43.9 0 0 43.9 0 97.8s43.9 97.8 97.8 97.8c36.5 0 68.3-20.1 85.1-49.7a97.76 97.76 0 0 0 149.6 25.4l19.4 22.2h3v-87.8h-80l24.3 27.5zM97.8 145c-26 0-47.1-21.1-47.1-47.1s21.1-47.1 47.1-47.1 47.2 21 47.2 47S123.8 145 97.8 145"
                  />
                </svg>
              </OglLogoWrapper>
              <FooterLicenceText>
                All content is available under the{' '}
                <a href="https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/" target="_blank" rel="noopener noreferrer">
                  Open Government Licence v3.0
                </a>
                , except where otherwise stated
              </FooterLicenceText>
            </FooterLicence>
          </Box>

          {/* Crown Copyright */}
          <CopyrightLogo>
            <CrestLogo />
            <CopyrightText href="https://www.nationalarchives.gov.uk/information-management/re-using-public-sector-information/uk-government-licensing-framework/crown-copyright/" target="_blank" rel="noopener noreferrer">
              © Crown copyright
            </CopyrightText>
          </CopyrightLogo>
        </FooterMeta>
      </FooterContainer>
    </FooterRoot>
  );
};

export default LayoutFooter;
