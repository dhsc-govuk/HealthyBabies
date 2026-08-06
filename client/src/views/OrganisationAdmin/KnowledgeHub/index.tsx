import React, { useEffect, useState } from 'react';
import { GeneralLayout } from '../../../layouts';
import { GovUKSideNavigation, GovUKInsetText, GovUKBreadcrumbs } from '../../../components/GovUKComponents';
import type { SideNavigationSection } from '../../../components/GovUKComponents';
import './styles.css';
import usePageTitle from '../../../hooks/usePageTitle';

const sections: SideNavigationSection[] = [
  {
    heading: 'Introduction',
    items: [{ text: 'About this guidance', href: '#introduction', id: 'introduction-link' }],
  },
  {
    heading: 'Core Data: Delivery Locations',
    items: [
      { text: 'About this section', href: '#fhs-about', id: 'fhs-about-link' },
      { text: 'FHS01: Delivery Site Name', href: '#fhs01', id: 'fhs01-link' },
      { text: 'FHS02: Postcode (Address)', href: '#fhs02', id: 'fhs02-link' },
      { text: 'FHS03: UPRN', href: '#fhs03', id: 'fhs03-link' },
      { text: 'FHS13: Address Line 2', href: '#fhs13', id: 'fhs13-link' },
      { text: 'FHS14: Town or City', href: '#fhs14', id: 'fhs14-link' },
      { text: 'FHS15: County', href: '#fhs15', id: 'fhs15-link' },
      { text: 'FHS04: Status of Site', href: '#fhs04', id: 'fhs04-link' },
      { text: 'FHS05: Site Closure Date', href: '#fhs05', id: 'fhs05-link' },
      { text: 'FHS06: Reason for Closure', href: '#fhs06', id: 'fhs06-link' },
      { text: 'FHS07: Type of Site', href: '#fhs07', id: 'fhs07-link' },
      { text: 'FHS07A: Date Planned to be Opened', href: '#fhs07a', id: 'fhs07a-link' },
      { text: 'FHS08: Date Opened or Repurposed', href: '#fhs08', id: 'fhs08-link' },
      { text: 'FHS08A: Location Repurposed', href: '#fhs08a', id: 'fhs08a-link' },
      { text: 'FHS08B: Previous Location Use', href: '#fhs08b', id: 'fhs08b-link' },
      { text: 'FHS09: BSFH Branding', href: '#fhs09', id: 'fhs09-link' },
      { text: 'FHS10: Location Type (Co-location)', href: '#fhs10', id: 'fhs10-link' },
      { text: 'FHS11: Location Type Select', href: '#fhs11', id: 'fhs11-link' },
      { text: 'FHS12: Clarification Comments', href: '#fhs12', id: 'fhs12-link' },
      { text: 'FHS17: Days Per Week Open', href: '#fhs17', id: 'fhs17-link' },
    ],
  },
  {
    heading: 'Core Data: Services',
    items: [
      { text: 'About this section', href: '#smd-about', id: 'smd-about-link' },
      { text: 'SMD01: Service Name', href: '#smd01', id: 'smd01-link' },
      { text: 'SMD02: Service Funding', href: '#smd02', id: 'smd02-link' },
      { text: 'SMD03: Service Status', href: '#smd03', id: 'smd03-link' },
      { text: 'SMD04: Service Status (No Longer Offered)', href: '#smd04', id: 'smd04-link' },
      { text: 'SMD05: Start Date', href: '#smd05', id: 'smd05-link' },
      { text: 'SMD06: Service Frequency', href: '#smd06', id: 'smd06-link' },
      { text: 'SMD07: Service Frequency (If Other)', href: '#smd07', id: 'smd07-link' },
      { text: 'SMD08: Strand', href: '#smd08', id: 'smd08-link' },
      { text: 'SMD09: Service Category (PIR/PMH)', href: '#smd09', id: 'smd09-link' },
      { text: 'SMD10: Service Category (Parenting)', href: '#smd10', id: 'smd10-link' },
      { text: 'SMD10A: Parenting EBI (approved exemptions)', href: '#smd10a', id: 'smd10a-link' },
      { text: 'SMD11: Service Category (HLE)', href: '#smd11', id: 'smd11-link' },
      { text: 'SMD11A: HLE EBI (approved exemptions)', href: '#smd11a', id: 'smd11a-link' },
      { text: 'SMD12: Wider Service Categories', href: '#smd12', id: 'smd12-link' },
      { text: 'SMD13: Lowest Age Benefit', href: '#smd13', id: 'smd13-link' },
      { text: 'SMD14: Highest Age Benefit', href: '#smd14', id: 'smd14-link' },
      { text: 'SMD15: Type of Service', href: '#smd15', id: 'smd15-link' },
      { text: 'SMD16: Delivery Methods', href: '#smd16', id: 'smd16-link' },
      { text: 'SMD17: Service Location', href: '#smd17', id: 'smd17-link' },
      { text: 'SMD18: Service Location (Other)', href: '#smd18', id: 'smd18-link' },
      { text: 'SMD19: Service Location: Specific', href: '#smd19', id: 'smd19-link' },
      { text: 'SMD20: Service Provider', href: '#smd20', id: 'smd20-link' },
      { text: 'SMD21: External Provider Names', href: '#smd21', id: 'smd21-link' },
    ],
  },
  {
    heading: 'Core Data: Wider Services',
    items: [
      { text: 'About this section', href: '#wsmd-about', id: 'wsmd-about-link' },
      { text: 'WSMD01: Wider Services', href: '#wsmd01', id: 'wsmd01-link' },
      { text: 'WSMD02: Delivery Location and Type', href: '#wsmd02', id: 'wsmd02-link' },
    ],
  },
  {
    heading: 'Section 1: Healthy Babies Offer and Parent-Carer Participation',
    items: [
      { text: 'About this section', href: '#spcp-about', id: 'spcp-about-link' },
      { text: 'SPCP01: Offer published', href: '#spcp01', id: 'spcp01-link' },
      { text: 'SPCP04: PCP meetings', href: '#spcp04', id: 'spcp04-link' },
      { text: 'SPCP05: PCP meetings (how many?)', href: '#spcp05', id: 'spcp05-link' },
      { text: 'SPCP06: PCP feedback', href: '#spcp06', id: 'spcp06-link' },
      { text: 'SPCP06a: PCP case study upload', href: '#spcp06a', id: 'spcp06a-link' },
      { text: 'SPCP07: PCP Panel numbers', href: '#spcp07', id: 'spcp07-link' },
      { text: 'SPCP08: PCP data availability - Sex', href: '#spcp08', id: 'spcp08-link' },
      { text: 'SPCP09: PCP data availability - Age', href: '#spcp09', id: 'spcp09-link' },
      { text: 'PCP10: PCP data availability - Ethnicity', href: '#pcp10', id: 'pcp10-link' },
    ],
  },
  {
    heading: 'Section 2: Quarterly Service Users',
    items: [
      { text: 'About this section', href: '#qsu-about', id: 'qsu-about-link' },
      { text: 'QSU01: Service status this quarter', href: '#qsu01', id: 'qsu01-link' },
      { text: 'QSU02: User number availability', href: '#qsu02', id: 'qsu02-link' },
      { text: 'QSU05: Number of service users - Overall', href: '#qsu05', id: 'qsu05-link' },
      { text: 'QSU06: Number of service users - Virtually', href: '#qsu06', id: 'qsu06-link' },
      { text: 'QSU07: De-duplication of service users', href: '#qsu07', id: 'qsu07-link' },
      { text: 'QSU08: Who has been counted', href: '#qsu08', id: 'qsu08-link' },
      { text: 'QSU04: Service users by ethnic group', href: '#qsu04', id: 'qsu04-link' },
      { text: 'QSU10: Service users by IDACI', href: '#qsu10', id: 'qsu10-link' },
      { text: 'QSU11: Service users by sex', href: '#qsu11', id: 'qsu11-link' },
      { text: 'QSU12: Service users by age', href: '#qsu12-age', id: 'qsu12-age-link' },
      { text: 'QSU13A: Service users by age group (availability)', href: '#qsu13a', id: 'qsu13a-link' },
      { text: 'QSU03: Pre post availability', href: '#qsu03', id: 'qsu03-link' },
      { text: 'QSU12: Waiting time', href: '#qsu12-waiting', id: 'qsu12-waiting-link' },
      { text: 'QSU13: Average waiting time', href: '#qsu13', id: 'qsu13-link' },
      { text: 'QSU15: Clarification comments (removed)', href: '#qsu15', id: 'qsu15-link' },
      { text: 'QSU18: EBI identification', href: '#qsu18', id: 'qsu18-link' },
      { text: 'QSU19: Attendance rates availability', href: '#qsu19', id: 'qsu19-link' },
      { text: 'QSU20: Attendance rates', href: '#qsu20', id: 'qsu20-link' },
      { text: 'QSU21: Clarification comments', href: '#qsu21', id: 'qsu21-link' },
    ],
  },
  {
    heading: 'Section 3: Wider Service Users',
    items: [
      { text: 'About this section', href: '#wsu-about', id: 'wsu-about-link' },
      { text: 'QWSU01: Wider service user number availability', href: '#qwsu01', id: 'qwsu01-link' },
      { text: 'QWSU01a: Unique service users', href: '#qwsu01a', id: 'qwsu01a-link' },
    ],
  },
  {
    heading: 'Section 4: Outcome Scores',
    items: [
      { text: 'About this section', href: '#pps-about', id: 'pps-about-link' },
      { text: 'PPS01: Outcome service', href: '#pps01', id: 'pps01-link' },
      { text: 'PPS02: Outcome measure', href: '#pps02', id: 'pps02-link' },
      { text: 'PPS03: Outcome ethnicity', href: '#pps03', id: 'pps03-link' },
      { text: 'PPS04: Outcome sex', href: '#pps04', id: 'pps04-link' },
      { text: 'PPS05: Outcome IDACI', href: '#pps05', id: 'pps05-link' },
      { text: 'PPS06: Pre-intervention PHQ-9', href: '#pps06', id: 'pps06-link' },
      { text: 'PPS07: Post-intervention PHQ-9', href: '#pps07', id: 'pps07-link' },
      { text: 'PPS08: Pre-intervention GAD-7', href: '#pps08', id: 'pps08-link' },
      { text: 'PPS09: Post-intervention GAD-7', href: '#pps09', id: 'pps09-link' },
      { text: 'PPS10: Pre-intervention SWEMWBS', href: '#pps10', id: 'pps10-link' },
      { text: 'PPS11: Post-intervention SWEMWBS', href: '#pps11', id: 'pps11-link' },
      { text: 'PPS12: Pre-intervention MORS-SF', href: '#pps12', id: 'pps12-link' },
      { text: 'PPS13: Post-intervention MORS-SF', href: '#pps13', id: 'pps13-link' },
      { text: 'PPS14: Pre-intervention ASQ-3', href: '#pps14', id: 'pps14-link' },
      { text: 'PPS15: Post-intervention ASQ-3', href: '#pps15', id: 'pps15-link' },
      { text: 'PPS16: Pre-intervention KPCS', href: '#pps16', id: 'pps16-link' },
      { text: 'PPS17: Post-intervention KPCS', href: '#pps17', id: 'pps17-link' },
      { text: 'PPS18: Pre-intervention HLE', href: '#pps18', id: 'pps18-link' },
      { text: 'PPS19: Post-intervention HLE', href: '#pps19', id: 'pps19-link' },
      { text: 'PPS20: Pre-intervention PSS', href: '#pps20', id: 'pps20-link' },
      { text: 'PPS22: Post-intervention PSS', href: '#pps22', id: 'pps22-link' },
      { text: 'PPS23: Pre-intervention CPRS-SF', href: '#pps23', id: 'pps23-link' },
      { text: 'PPS24: Post-intervention CPRS-SF', href: '#pps24', id: 'pps24-link' },
      { text: 'PPS19: Pre/Post-intervention SDQ', href: '#pps19-sdq', id: 'pps19-sdq-link' },
      { text: 'PPS20: Pre/Post-intervention DASS-21', href: '#pps20-dass21', id: 'pps20-dass21-link' },
      { text: 'PPS21: Pre/Post-intervention Parenting Scale', href: '#pps21', id: 'pps21-link' },
      { text: 'PPS25: Pre-intervention other measure', href: '#pps25', id: 'pps25-link' },
      { text: 'PPS26: Post-intervention other measure', href: '#pps26', id: 'pps26-link' },
      { text: 'PPS27: Outcome case study', href: '#pps27', id: 'pps27-link' },
      { text: 'PPS28: Clarification comments', href: '#pps28', id: 'pps28-link' },
    ],
  },
  {
    heading: 'Section 5: Breastfeeding Rates',
    items: [
      { text: 'About this section', href: '#br-about', id: 'br-about-link' },
      { text: 'BR01: Breastfeeding rates at initiation', href: '#br01', id: 'br01-link' },
      { text: 'BR02: Breastfeeding rates at 6-8 weeks', href: '#br02', id: 'br02-link' },
      { text: 'BR03: Comments', href: '#br03', id: 'br03-link' },
    ],
  },
  {
    heading: 'Section 6: Hub Users',
    items: [
      { text: 'About this section', href: '#bsfh-about', id: 'bsfh-about-link' },
      { text: 'BSFH01: Hub user number availability', href: '#bsfh01', id: 'bsfh01-link' },
      { text: 'BSFH02: Which hubs can you provide numbers for', href: '#bsfh02', id: 'bsfh02-link' },
      { text: 'BSFH02A: Specify hub names', href: '#bsfh02a', id: 'bsfh02a-link' },
      { text: 'BSFH03: Number of hub users - Overall', href: '#bsfh03', id: 'bsfh03-link' },
      { text: 'BSFH04: De-duplication of hub users', href: '#bsfh04', id: 'bsfh04-link' },
      { text: 'BSFH05: Who has been counted', href: '#bsfh05', id: 'bsfh05-link' },
      { text: 'BSFH06: Hub users by location availability', href: '#bsfh06', id: 'bsfh06-link' },
      { text: 'BSFH07: Hub users by location', href: '#bsfh07', id: 'bsfh07-link' },
      { text: 'BSFH08: Hub users by ethnicity', href: '#bsfh08', id: 'bsfh08-link' },
      { text: 'BSFH10: Hub users by IDACI', href: '#bsfh10', id: 'bsfh10-link' },
      { text: 'BSFH12: Hub users by sex', href: '#bsfh12', id: 'bsfh12-link' },
      { text: 'BSFH14: Hub users by age', href: '#bsfh14', id: 'bsfh14-link' },
    ],
  },
];

const sectionTitleMap: Record<string, string> = sections
  .flatMap((s) => s.items)
  .reduce(
    (map, item) => {
      map[item.href.replace('#', '')] = item.text;
      return map;
    },
    {} as Record<string, string>
  );

const KnowledgeHub = (): React.ReactElement => {
  const [activeSection, setActiveSection] = useState<string>('introduction');
  usePageTitle(sectionTitleMap[activeSection] ? `${sectionTitleMap[activeSection]} - Guidance` : 'Guidance');

  useEffect(() => {
    const observerOptions: IntersectionObserverInit = {
      root: null,
      rootMargin: '-20% 0px -70% 0px',
      threshold: 0,
    };

    const observerCallback: IntersectionObserverCallback = (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          const sectionId = entry.target.getAttribute('data-section-id');
          if (sectionId) {
            setActiveSection(sectionId);
          }
        }
      });
    };

    const observer = new IntersectionObserver(observerCallback, observerOptions);

    const articles = document.querySelectorAll('[data-section-id]');
    articles.forEach((article) => observer.observe(article));

    return () => {
      observer.disconnect();
    };
  }, []);

  return (
    <GeneralLayout currentPage="">
      <div className="govuk-grid-row knowledge-hub-row">
        <div className="govuk-grid-column-one-third knowledge-hub-sidebar-column">
          <nav className="knowledge-hub-sidebar" aria-label="Knowledge hub sections">
            <h2 className="govuk-heading-s govuk-!-margin-bottom-3">Contents</h2>
            <GovUKSideNavigation label="Guidance navigation" sections={sections} activeSection={activeSection} />
          </nav>
        </div>

        <div className="govuk-grid-column-two-thirds">
          <div className="knowledge-hub-content">
            <GovUKBreadcrumbs
              items={[
                { label: 'Home', href: '/organisation-admin/home' },
                { label: 'Guidelines', href: '/organisation-admin/help' },
              ]}
            />
            <h1 className="govuk-heading-xl">Guidance</h1>
            <p className="govuk-body-l">
              Guidance for the quarterly Management Information (MI) collection for Best Start Family Hubs and Healthy Babies, aligned to Department of Health and Social Care
              (DHSC) policy.
            </p>

            {/* Introduction */}
            <section aria-labelledby="introduction-heading">
              <article id="introduction" data-section-id="introduction" tabIndex={-1}>
                <h2 className="govuk-heading-l" id="introduction-heading">
                  About this Guidance
                </h2>
                <p className="govuk-body">
                  This guidance provides help for the data you are asked to enter as part of the quarterly MI collection. It is grouped by section and by question code so you can
                  find guidance for the specific question you are answering.
                </p>
                <p className="govuk-body">Use the contents menu on the left to jump to a section or to a particular question.</p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Core Data: Delivery Locations */}
            <section aria-labelledby="fhs-section-heading">
              <h2 className="govuk-heading-l" id="fhs-section-heading" data-section-id="fhs">
                Core Data: Delivery Locations
              </h2>

              <article id="fhs-about" data-section-id="fhs-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">
                  Provide information about the locations through which you deliver Best Start Family Hub and Healthy Babies services. This includes Best Start Family Hubs,
                  locations working towards Best Start Family Hub status, Family Hubs (for locations open prior to 01/04/2026), and Network Sites. You can also provide information
                  for locations planned to open in the future.
                </p>
                <GovUKInsetText>
                  This section is <strong>always open for editing</strong> and should be treated as <strong>live data</strong>. This section must be completed before any other core
                  data or quarterly return. The locations listed here will be used for responses in other modules.
                </GovUKInsetText>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs01" data-section-id="fhs01" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS01: Delivery Site Name</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the official public name of the location?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> The official public name is how the location is referred to locally. It may not match the address based on postcode, particularly if a
                  location has been renamed to a Best Start Family Hub. This will be used to update the published list of Best Start Family Hubs. Only hubs meeting the BSFH
                  definition will appear on the published list.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs02" data-section-id="fhs02" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS02: Postcode (Address)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the location&rsquo;s postcode?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> In the online form, providing the postcode will prompt the exact address via a drop-down menu. A postcode must be provided.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs03" data-section-id="fhs03" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS03: Unique Property Reference Number (UPRN)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the location&rsquo;s UPRN?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> In the online form, this is auto-filled from the postcode and address. The UPRN is a national unique identifier for every addressable
                  location in Great Britain. It is used to match data to other datasets (e.g., school census). For bulk upload templates, source the UPRN via the{' '}
                  <a className="govuk-link" href="https://osdatahub.os.uk/" target="_blank" rel="noopener noreferrer">
                    Ordnance Survey Data Hub
                  </a>{' '}
                  (free account required).
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs13" data-section-id="fhs13" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS13: Address Line 2 (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the address line 2 of the location?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Auto-filled in the online form. Optional field in the bulk upload CSV.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs14" data-section-id="fhs14" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS14: Town or City</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the town or city the location is in?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Auto-filled in the online form. Must be entered in the bulk upload CSV according to the official address.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs15" data-section-id="fhs15" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS15: County (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the county the location is in?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Auto-filled in the online form. Optional field in the bulk upload CSV.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs04" data-section-id="fhs04" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS04: Status of Site</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the status of the location?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong>
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    <strong>The location is currently open</strong> — currently delivering BSFH and/or Healthy Babies services in any capacity.
                  </li>
                  <li>
                    <strong>The location is planned to be opened</strong> — the location is due to open in the future.
                  </li>
                  <li>
                    <strong>The location has been closed</strong> — the location has closed or stopped operating as a BSFH, Family Hub, or Network Site.
                  </li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs05" data-section-id="fhs05" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS05: Site Closure Date</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What date did the location close?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For locations that have closed, provide the closure date. If the date falls within a reporting quarter, the location will still be
                  available for that quarter&rsquo;s return but will not appear in subsequent quarterly returns.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs06" data-section-id="fhs06" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS06: Reason for Closure (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Why did the location close?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Optional free-text box for additional information about the closure.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs07" data-section-id="fhs07" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS07: Type of Site</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What type of location is it? (Select all that apply)
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>A Best Start Family Hub</li>
                  <li>Working towards becoming a Best Start Family Hub</li>
                  <li>A network site</li>
                </ul>
                <p className="govuk-body">
                  <strong>Definitions:</strong>
                </p>
                <p className="govuk-body">
                  <strong>Best Start Family Hub</strong> — A site should:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    Be a physical place where families can meet trained staff for clear advice on a wide range of family issues spanning the 0–19 age range (25 with SEND), and be
                    signposted to targeted or specialist support.
                  </li>
                  <li>
                    Have trained staff to connect families to a range of 0–5 universal health and family services, alongside more intensive support, with a particular focus on
                    supporting babies and driving progress towards the Good Level of Development target.
                  </li>
                  <li>Where appropriate, co-locate services within the BSFH.</li>
                  <li>
                    Have on-site professional support for parents/carers of children with additional needs, and for evidence-based parenting and HLE interventions for families with
                    3–4-year-olds to boost school readiness.
                  </li>
                  <li>
                    Have on-site support for infant feeding, perinatal mental health, and parent-infant relationships (for LAs receiving Healthy Babies funding), and information
                    available to support families from conception to age 2.
                  </li>
                  <li>Have outreach services linked to the hub site, supported by a locally developed outreach strategy and an accessible digital offer.</li>
                  <li>Ensure parents, carers, and VCFS organisations are involved in shaping the BSFH offer.</li>
                  <li>
                    Use the naming convention <strong>Best Start Family Hubs and Healthy Babies</strong> and display <strong>Best Start in Life (BSiL) branding</strong>, alongside
                    local brands as appropriate.
                  </li>
                </ul>
                <p className="govuk-body">
                  <strong>Working towards a Best Start Family Hub</strong> — Sites currently open and delivering services, intending to become a BSFH but missing some required
                  criteria and actively working to address them. If the site is also a Network Site or Family Hub, select both relevant options. If the site does not meet the
                  Network Site or Family Hub definition, select only &lsquo;Working towards a Best Start Family Hub&rsquo;.
                </p>
                <p className="govuk-body">
                  <strong>Family Hub</strong> — Applicable only to sites operating prior to 1st April 2026 that met either Part A or Part B of the Family Hub definition. Family
                  Hubs are expected to transition to Best Start Family Hubs by March 2028.
                </p>
                <p className="govuk-body">
                  <em>2022/23–25/26 Family Hub definition:</em>
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    <strong>Part A:</strong> A physical place where families can visit and speak to a trained staff member face-to-face, who provides advice on a wide range of
                    family issues (0–19, or 25 with SEND) and connects families to further services.
                  </li>
                  <li>
                    <strong>Part B:</strong> Families accessing parenting support, parent–infant relationships, perinatal mental health, early language, HLE, and infant feeding
                    have access to a key contact within the FH. The site uses the &lsquo;Family Hub&rsquo; naming convention and meets the 2025/26 Programme Guide requirements.
                  </li>
                </ul>
                <p className="govuk-body">
                  <strong>Network Site</strong> — Other trusted community locations that extend reach. They deliver at least one BSFH or Healthy Babies service on a regular,
                  sustained basis and can connect families with others. These might include early years settings, schools, libraries, health and faith venues, leisure centres, and
                  VCF venues.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs07a" data-section-id="fhs07a" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS07A: Date Planned to be Opened</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> When is the location planned to open as a BSFH or Network Site?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For locations with &lsquo;planned to be opened&rsquo; status, provide the expected opening date.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs08" data-section-id="fhs08" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS08: Date Opened or Repurposed into a Best Start Family Hub</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> When was the location officially opened as a hub or network site?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> This refers to when the location was first opened or repurposed as a BSFH or Network Site, according to the 2026 guidance definitions.
                  Not required for Family Hubs opened prior to 1st April 2026.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs08a" data-section-id="fhs08a" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS08A: Location Repurposed</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Was the location used for another purpose prior to being opened as a BSFH?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For Best Start Family Hubs only. If previously used for another purpose (e.g., Children&rsquo;s Centre, Library, Family Hub), select
                  &lsquo;yes&rsquo;.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Yes</li>
                  <li>No</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs08b" data-section-id="fhs08b" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS08B: Previous Location Use</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was the previous use of the location?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If FHS08A is &lsquo;yes&rsquo;, indicate the prior use:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Family Hub</li>
                  <li>Children&rsquo;s Centre</li>
                  <li>Library</li>
                  <li>Other</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs09" data-section-id="fhs09" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS09: BSFH Branding</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Has the location used Best Start in Life campaign branding? (Yes / No / Not yet)
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Use of BSiL campaign branding is a requirement of meeting the BSFH definition. If designated as a BSFH, the answer should be
                  &lsquo;yes&rsquo;. For locations transitioning to BSFH with branding planned but not yet implemented, select &lsquo;not yet&rsquo;.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs10" data-section-id="fhs10" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS10: Location Type (Co-location)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Does the hub share its location with another community organisation? (Yes / No)
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> &lsquo;Co-location&rsquo; means sharing the same physical space as another service. A service user does not need to physically move
                  between the two services for it to count as co-located. For example, a hub annexed to a primary school with separate entrances but a shared postcode/address is
                  considered co-located.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs11" data-section-id="fhs11" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS11: Location Type Select</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What community location(s) does the hub share its location with? (Select all that apply)
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>A building with other NHS/ICB services</li>
                  <li>A GP practice</li>
                  <li>A hospital</li>
                  <li>A library</li>
                  <li>Another local authority service</li>
                  <li>A school</li>
                  <li>A school-based nursery (SBN)</li>
                  <li>A Private, Voluntary or Independent (PVI) early years setting</li>
                  <li>A multi-aspect building for local authority services</li>
                  <li>A building with non-LA/non-NHS/non-ICB occupants</li>
                  <li>Other (free text)</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs12" data-section-id="fhs12" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS12: Clarification Comments (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Is there anything else we need to know about the hub?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Optional free-text field for additional context.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="fhs17" data-section-id="fhs17" tabIndex={-1}>
                <h3 className="govuk-heading-m">FHS17: Days Per Week Open</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many days per week is the location open?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Provide the number of days per week the location is typically open to deliver BSFH and/or Healthy Babies services.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>1</li>
                  <li>2</li>
                  <li>3</li>
                  <li>4</li>
                  <li>5</li>
                  <li>6</li>
                  <li>7</li>
                </ul>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Core Data: Services */}
            <section aria-labelledby="smd-section-heading">
              <h2 className="govuk-heading-l" id="smd-section-heading" data-section-id="smd">
                Core Data: Services
              </h2>

              <article id="smd-about" data-section-id="smd-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">This page lets you add, view, change or delete services offered through the Best Start Family Hub Network.</p>
                <p className="govuk-body">A service should be recorded if it meets the following definition:</p>
                <GovUKInsetText>
                  It is a service or intervention delivered by, or on behalf of, a Local Authority through their Best Start Family Hub Network which delivers the objectives set out
                  in the Best Start Family Hub &amp; Healthy Babies guidance.
                </GovUKInsetText>
                <p className="govuk-body">Those objectives are:</p>
                <ol className="govuk-list govuk-list--number">
                  <li>Provide support to parents and carers so they can nurture their babies and children, improving health and education outcomes for all.</li>
                  <li>
                    Contribute to a reduction in inequalities in health and education outcomes across England by ensuring support is communicated to all parents and carers,
                    including those hardest to reach and/or most in need.
                  </li>
                  <li>
                    Build the evidence base for what works when it comes to improving health and education outcomes for babies, children and families in different delivery
                    contexts.
                  </li>
                </ol>
                <p className="govuk-body">
                  <strong>A Best Start Family Hub network</strong> is defined as the totality of sites, partners, and physical, virtual, and outreach services connected to the Best
                  Start Family Hub.
                </p>
                <p className="govuk-body">A service may be considered to be delivered through the BSFH network if:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>It is delivered in a location meeting the Family Hub or BSFH definition.</li>
                  <li>It is delivered by staff employed through the BSFH.</li>
                  <li>Service users are referred to it through the BSFH as part of a joined-up approach.</li>
                </ul>
                <p className="govuk-body">
                  <strong>Key rules:</strong>
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    Funded or partially funded services should be reported at the <strong>service component level</strong> where possible (e.g., separate entries for
                    &lsquo;Incredible Years: autism&rsquo; and &lsquo;Incredible Years: baby&rsquo;).
                  </li>
                  <li>Core data includes both nationally and locally delivered services, not just those funded by BSFH/HB.</li>
                  <li>Where information is not available, respond &lsquo;unknown&rsquo; where this option is available.</li>
                  <li>Outreach activity should be recorded as a service (include the term &lsquo;outreach&rsquo; in the name).</li>
                  <li>
                    Services can be updated at any time and should be treated as <strong>live data</strong>.
                  </li>
                  <li>
                    <strong>This module must be completed before completing the quarterly return.</strong>
                  </li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd01" data-section-id="smd01" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD01: Service Name</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the name of the service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> The service name must be unique. Duplicate service names are not permitted.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd02" data-section-id="smd02" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD02: Service Funding</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Is the service funded by Best Start Family Hubs and/or Healthy Babies?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Record as funded or partially funded where funding comes from BSFH and/or Healthy Babies. Funded strands are:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Parenting support</li>
                  <li>Parent-infant relationships and perinatal mental health support</li>
                  <li>Early language and the home learning environment (HLE)</li>
                  <li>Infant feeding support</li>
                  <li>Children with additional needs and their families (available from 01/04/2026)</li>
                </ul>
                <p className="govuk-body">
                  Review your Delivery Plan to ensure all funded services are recorded. There should be <strong>at least one funded service per funding strand</strong>.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Funded</li>
                  <li>Partially funded</li>
                  <li>Not programme funded</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd03" data-section-id="smd03" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD03: Service Status</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the status of the service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong>
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    <strong>Open</strong> — currently available to service users, following the expected frequency of offer. Services delivered as a single annual event or
                    infrequently that have run previously and are intended to continue should be recorded as open.
                  </li>
                  <li>
                    <strong>Planned</strong> — for services intended to start later in the year.
                  </li>
                  <li>
                    <strong>Closed</strong> — the service is no longer running
                  </li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd04" data-section-id="smd04" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD04: Service Status (No Longer Offered)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> When did the service close?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Provide the date the service ceased to be offered to users.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd05" data-section-id="smd05" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD05: Start Date</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> When is the service expected to start?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For services designated as &lsquo;planned for implementation&rsquo;, provide the planned start date.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd06" data-section-id="smd06" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD06: Service Frequency</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How frequently can users access this service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Determined by how frequently a user could access the service if they attended every available instance in the quarter. Examples:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    Service delivered in multiple locations on the same day → <strong>more than daily</strong>
                  </li>
                  <li>
                    Service runs once a week for six weeks, once in the quarter → <strong>weekly</strong>
                  </li>
                  <li>
                    Websites, apps, 24/7 helplines → <strong>always live</strong>
                  </li>
                  <li>
                    Helplines available during working hours only → <strong>daily</strong>
                  </li>
                </ul>
                <p className="govuk-body">Select one:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>More than once a day</li>
                  <li>Daily</li>
                  <li>Weekly</li>
                  <li>Monthly</li>
                  <li>Annually</li>
                  <li>Only once</li>
                  <li>As needed</li>
                  <li>At any time (for example, using a helpline or website)</li>
                  <li>Other</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd07" data-section-id="smd07" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD07: Service Frequency (If Other) (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Provide more information about how often users can attend this service.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Use only if available frequency options do not adequately describe the service.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd08" data-section-id="smd08" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD08: Strand</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Which strand funds this service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For funded or partially funded services, select <strong>one</strong> strand:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Parenting support</li>
                  <li>Parent-infant relationships and perinatal mental health support</li>
                  <li>Infant Feeding support</li>
                  <li>Early language and the home learning environment</li>
                  <li>Children with additional needs and their families</li>
                  <li>Delivery</li>
                  <li>Wider services</li>
                </ul>
                <p className="govuk-body">
                  If funded by multiple strands, select the strand contributing the majority of funding. If not funded by BSFH/HB, record as <strong>Wider Services</strong>.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd09" data-section-id="smd09" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD09: Service Category (Parent-Infant Relationships / Perinatal Mental Health)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Which category does this service belong to? (Select all that apply)
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Parent-Infant Relationship (PIR) Support</li>
                  <li>Perinatal Mental Health – Mild-Moderate Support</li>
                  <li>Perinatal Mental Health – Support for Dads and Co-Parents</li>
                  <li>Other</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd10" data-section-id="smd10" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD10: Service Category (Parenting)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> If the parenting support service is from the Evidence Based Intervention (EBI) menu, select the relevant intervention.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For parenting support strand services only. Most parenting services are expected to be delivered as a component of an EBI from the
                  menu. If an exemption has been agreed with DfE for a non-EBI service, select <strong>Not Applicable</strong>.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Not applicable</li>
                  <li>Child-Parent Psychotherapy</li>
                  <li>Empowering Parents Empowering Communities (EPEC)</li>
                  <li>Family Check-Up for Children (FCU)</li>
                  <li>Generation Parent Management Training Oregon (GEN PMTO)</li>
                  <li>Incredible Years Preschool</li>
                  <li>Incredible Years Preschool +ADVANCE Parent Training Curriculum</li>
                  <li>ParentChild+</li>
                  <li>Parents as First Teachers (PAFT)</li>
                  <li>Schoolchildren and their Families</li>
                  <li>Strengthening Families, Strengthening Communities</li>
                  <li>Trauma-Focused Cognitive Behavioural Therapy</li>
                  <li>Triple P &ndash; Enhanced Triple P (Level 5)</li>
                  <li>Triple P &ndash; Family Transitions</li>
                  <li>Triple P &ndash; Level 4 Group Triple P</li>
                  <li>Triple P &ndash; Level 4 Standard Triple P</li>
                  <li>Triple P &ndash; Level 5 Pathways</li>
                  <li>Triple P &ndash; Selected (Seminars) Stepping Stones</li>
                  <li>Triple P &ndash; Standard Stepping Stones + Group</li>
                  <li>Triple P Level 3 Discussion Groups</li>
                  <li>Triple P Online</li>
                  <li>Video-Interventions to promote positive parenting and sensitive discipline (VIPPSD)</li>
                  <li>Healthy Start, Happy Start</li>
                  <li>Exemption (other)</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd10a" data-section-id="smd10a" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD10A: Parenting EBI (approved exemptions)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> If the parenting support service is an approved exemption from the Evidence Based Intervention (EBI) menu, select the relevant approved
                  exemption.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Select the approved exemption that has been agreed with DfE for this parenting support service.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>The Nurturing Programme (Centre for Emotional Health)</li>
                  <li>Invest in Play</li>
                  <li>Understanding Your Child (Togetherness)</li>
                  <li>The Parents Plus Early Years Programme</li>
                  <li>Mellow Growing Together - Parenting Group</li>
                  <li>Theraplay (aimed at children aged 3 to 4)</li>
                  <li>Approachable Parenting / 5 Pillars of Parenting</li>
                  <li>Fathers Ready Every Day (FRED) / Fatherhood Champions programme</li>
                  <li>Understanding Your Child - Online (Togetherness)</li>
                  <li>Mellow Toddlers</li>
                  <li>Video Interactive Guidance</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd11" data-section-id="smd11" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD11: Service Category (HLE)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> If the Early Language and HLE service is from the EBI menu, select the relevant intervention.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For Early Language and HLE strand services only. If an exemption has been agreed with DfE for a non-EBI service, select{' '}
                  <strong>Not Applicable</strong>.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Not applicable</li>
                  <li>Auditory Verbal Therapy</li>
                  <li>Early Talk Boost</li>
                  <li>Early Words Together</li>
                  <li>EasyPeasy</li>
                  <li>Incredible Years Preschool</li>
                  <li>Incredible Years Preschool +ADVANCE Parent Training Curriculum</li>
                  <li>Let&rsquo;s Play in Tandem</li>
                  <li>LEAP into Learning</li>
                  <li>Making it REAL</li>
                  <li>ParentChild+</li>
                  <li>Parents as First Teachers (PAFT)</li>
                  <li>PEEP Learning Together Programme</li>
                  <li>Triple P &ndash; Level 3 Discussion Groups</li>
                  <li>Triple P &ndash; Level 4 Group</li>
                  <li>Triple P &ndash; Online</li>
                  <li>Triple P &ndash; Stepping Stones</li>
                  <li>Exemption (other)</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd11a" data-section-id="smd11a" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD11A: HLE EBI (approved exemptions)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> If the Early Language and HLE service is an approved exemption from the EBI menu, select the relevant approved exemption.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Select the approved exemption that has been agreed with DfE for this Early Language and HLE service.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Parenting Puzzle (Centre for Emotional Health)</li>
                  <li>Tots Talking (Speech and Language UK)</li>
                  <li>Let&rsquo;s Talk Read Play</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd12" data-section-id="smd12" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD12: Wider Service Categories</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Does this service fall into any wider categories?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> The following wider services <strong>always</strong> meet the definition for core services data and <strong>must</strong> be reported
                  here:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>None of these</li>
                  <li>Activities for children aged 0-5</li>
                  <li>Health visiting (ages 0-5)</li>
                  <li>Local authority 0-19 public health services</li>
                  <li>Midwifery/maternity and neonatal services</li>
                  <li>Healthy growth and nutrition support</li>
                  <li>Vaccination services</li>
                  <li>Birth Registration</li>
                  <li>Children affected by parent/carer imprisonment</li>
                  <li>Reducing parent/carer conflict</li>
                  <li>Support for separated and separating parents/carers</li>
                  <li>Targeted whole-family support, delivered by local family help services</li>
                  <li>Unknown</li>
                </ul>
                <p className="govuk-body">
                  The following wider services <strong>may or may not</strong> meet the definition — report here or under &lsquo;Wider Services Categories&rsquo; based on local
                  decision:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Mental health services (beyond Start for Life parent-infant mental health)</li>
                  <li>Oral health improvement</li>
                  <li>Stop smoking support</li>
                  <li>Substance (alcohol/drug) misuse support</li>
                  <li>Domestic Abuse support</li>
                  <li>Debt and welfare advice</li>
                  <li>Housing</li>
                  <li>Youth justice services</li>
                  <li>Youth services – universal and targeted</li>
                  <li>Food support</li>
                  <li>Social welfare advice services</li>
                  <li>Support for school attendance</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd13" data-section-id="smd13" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD13: Lowest Age Benefit</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the lowest age of children expected to benefit from the service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For example, a service for parents of children aged 0–5, the lowest age category would be 0–2. For antenatal services, select{' '}
                  <strong>up to birth</strong>.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Up to birth</li>
                  <li>0-2 years</li>
                  <li>3-4 years</li>
                  <li>5-11 years</li>
                  <li>12-17 years</li>
                  <li>18-25 years</li>
                  <li>26 and older</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd14" data-section-id="smd14" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD14: Highest Age Benefit</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the highest age of children expected to benefit from the service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For example, a service for parents of children aged 0–4, the highest age category would be 3–4. For antenatal-only services, the
                  highest age would be <strong>up to birth</strong>.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Up to birth</li>
                  <li>0-2 years</li>
                  <li>3-4 years</li>
                  <li>5-11 years</li>
                  <li>12-17 years</li>
                  <li>18-25 years</li>
                  <li>26 and older</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd15" data-section-id="smd15" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD15: Type of Service</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Is this a targeted, specialist, or universal service? (Select all that apply)
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    <strong>Targeted</strong> — Support designed for a specific group with particular challenges or conditions requiring more focused attention than universal
                    services provide. E.g., parenting courses targeted at financially insecure families.
                  </li>
                  <li>
                    <strong>Specialist</strong> — Support for people with rare and complex conditions. E.g., specialist staff supporting families where a parent or baby has a
                    congenital condition requiring additional support.
                  </li>
                  <li>
                    <strong>Universal</strong> — Publicly available services provided to every family regardless of background or specific needs.
                  </li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd16" data-section-id="smd16" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD16: Delivery Methods</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How is this service delivered? (Select all that apply)
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>In-person coordinator-led group sessions</li>
                  <li>In-person one-to-one sessions</li>
                  <li>Virtual coordinator-led group sessions</li>
                  <li>Virtual one-to-one sessions</li>
                  <li>Clinical appointments or clinical drop-in</li>
                  <li>Community/drop-in/awareness event</li>
                  <li>Peer support</li>
                  <li>Home visits</li>
                  <li>A mobile app</li>
                  <li>A website</li>
                  <li>A telephone helpline</li>
                  <li>Other</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd17" data-section-id="smd17" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD17: Service Location</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Where is this service being delivered?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Provide all location types where the service is delivered. For Wider Services delivered through BSFH and other community locations
                  (e.g., midwifery delivered in hospitals, homes, and community settings), provide the delivery locations you are aware of — the list need not be exhaustive.
                </p>
                <p className="govuk-body">Select all that apply:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>All Best Start Family Hubs</li>
                  <li>All network sites</li>
                  <li>Some Best Start Family Hubs or network locations</li>
                  <li>Home visits</li>
                  <li>Early years setting</li>
                  <li>Primary school</li>
                  <li>Hospital</li>
                  <li>Virtual</li>
                  <li>Other</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd18" data-section-id="smd18" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD18: Service Location (Other) (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Tell us more about where this service is being delivered.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Optional additional information about the delivery location.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd19" data-section-id="smd19" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD19: Service Location: Specific</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Which locations offer this service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Required if &lsquo;some hub sites&rsquo; or &lsquo;some network sites&rsquo; is selected in SMD17. Specify locations using the delivery
                  location names from your Core Delivery Locations data. Names must match exactly to avoid validation errors on bulk upload.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd20" data-section-id="smd20" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD20: Service Provider</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Who is responsible for delivering this service? (Select all that apply)
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Required for all funded and partially funded services. For Wider or Unfunded services where information is not available, select{' '}
                  <strong>unknown</strong>.
                </p>
                <p className="govuk-body">Select all that apply:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>A commissioned delivery partner</li>
                  <li>A commissioned VCFS partner</li>
                  <li>ICB/NHS</li>
                  <li>Local Authority</li>
                  <li>A non-commissioned VCFS partner</li>
                  <li>Other</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="smd21" data-section-id="smd21" tabIndex={-1}>
                <h3 className="govuk-heading-m">SMD21: External Provider Names (optional)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the name of the organisation delivering the service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Optional. For funded or partially funded services not delivered by the Local Authority, provide the name of the delivering
                  organisation. This supports analysis of partner organisations and stakeholder engagement.
                </p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Core Data: Wider Services */}
            <section aria-labelledby="wsmd-section-heading">
              <h2 className="govuk-heading-l" id="wsmd-section-heading" data-section-id="wsmd">
                Core Data: Wider Services
              </h2>

              <article id="wsmd-about" data-section-id="wsmd-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">This page lets you add, view, change or delete wider services offered through your Best Start Family Hub Network.</p>
                <GovUKInsetText>If you have reported all delivery for a wider service category in your Core Service Data, you do not need to report on it here.</GovUKInsetText>
                <p className="govuk-body">
                  You can add or update services via the guided online form or by uploading a CSV file. Only add categories not covered or only partially covered in your Core
                  Service data.
                </p>
                <p className="govuk-body">
                  <strong>How to add or update using the guided form:</strong>
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>
                    Select <strong>Add</strong> to add a wider service from the pre-defined categories.
                  </li>
                  <li>
                    Select <strong>Change</strong> to update a single wider service.
                  </li>
                  <li>Save progress at any time and return later.</li>
                </ul>
                <p className="govuk-body">
                  <strong>How to add or update using a CSV:</strong>
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Download the service list template as CSV or XLSM from this page.</li>
                  <li>Add details for one or more services.</li>
                  <li>Leave a wider service category row blank if you do not want to report on it.</li>
                  <li>Save as CSV and upload via &lsquo;Upload with CSV&rsquo;.</li>
                </ul>
                <p className="govuk-body">
                  Beyond funded services, LAs are expected to integrate wider services into their BSFH model. Where delivery of wider services{' '}
                  <strong>does not contribute to the objectives</strong> of BSFH and Healthy Babies, those services can be reported here.
                </p>
                <p className="govuk-body">
                  Wider services in this section are collected at an <strong>aggregate level</strong> (e.g., all stop smoking support reported under one category). Not all wider
                  services from the guidance are available here — some always meet the objectives (e.g., Health Visiting) and must be reported in the main Services Core Data.
                </p>
                <p className="govuk-body">The wider services available to report in this section are:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Mental health services (beyond perinatal mental health and parent-infant relationships)</li>
                  <li>Oral health improvement</li>
                  <li>Stop smoking support</li>
                  <li>Substance (alcohol/drug) misuse support</li>
                  <li>Domestic Abuse support</li>
                  <li>Debt advice, money guidance and welfare advice</li>
                  <li>Housing</li>
                  <li>Youth justice services</li>
                  <li>Youth services – universal and targeted</li>
                  <li>Food support</li>
                  <li>Social welfare advice services</li>
                  <li>Support for school attendance</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="wsmd01" data-section-id="wsmd01" tabIndex={-1}>
                <h3 className="govuk-heading-m">WSMD01: Wider Services</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do your delivery locations offer any of these wider services?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For each wider service category, respond <strong>yes</strong> if any component of the wider service is offered through a BSFH,
                  &lsquo;working toward a BSFH&rsquo;, Family Hub, or Network Site.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="wsmd02" data-section-id="wsmd02" tabIndex={-1}>
                <h3 className="govuk-heading-m">WSMD02: Delivery Location and Type</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Where are these wider services delivered?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Specify through which locations the wider service is delivered. If you report delivery through the BSFH network, you will be prompted
                  to provide quarterly service user totals for that wider service category.
                </p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Section 1: Healthy Babies Offer and Parent-Carer Participation */}
            <section aria-labelledby="spcp-section-heading">
              <h2 className="govuk-heading-l" id="spcp-section-heading" data-section-id="spcp">
                Section 1: Healthy Babies Offer and Parent-Carer Participation
              </h2>

              <article id="spcp-about" data-section-id="spcp-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">Tell us about the ways in which you have engaged with parents and carers.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp01" data-section-id="spcp01" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP01: Offer published</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Have you published information about your Healthy Babies Offer? Please select one option.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> From 2022/23 – 25/26 this was referred to as the &lsquo;Start for Life Offer&rsquo;. The Healthy Babies Offer is defined in the Best
                  Start Family Hub &amp; Healthy Babies guidance, which sets out that:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>the Offer should be published in a single, accessible online space, so that families can easily find out what support is available to them.</li>
                  <li>All parents-to-be should be provided with a hard-copy version of the local Healthy Babies offer prior to birth.</li>
                </ul>
                <p className="govuk-body">Respond with how your Offer was published, whether it was referred to as the Start for Life Offer or Healthy Babies Offer.</p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Yes - both physically and online</li>
                  <li>Yes - physically only</li>
                  <li>Yes - online only</li>
                  <li>No</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp04" data-section-id="spcp04" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP04: PCP meetings</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How do you engage with parents and carers? Please select all which apply.
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Parent-Carer Panel Meetings</li>
                  <li>Online group or forum</li>
                  <li>Feedback surveys through hubs or services</li>
                  <li>Community drop-in events</li>
                  <li>Co-design sessions</li>
                  <li>Other</li>
                  <li>None</li>
                </ul>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Select all ways that apply in which your Local Authority has engaged with parents and carers. This may be through formal parent-carer
                  panels, or other routes.
                </p>
                <p className="govuk-body">
                  From 2022/23 – 25/26 Local Authorities were expected to organise formal parent-carer panels. From 2026/27, the Best Start Family Hub &amp; Healthy Babies guidance
                  sets out that Local Authorities should create clear, inclusive and diverse routes for parent and carer participation to shape how Best Start Family Hubs and
                  Healthy Babies services are designed, delivered and improved. This does not necessarily need to take the form of a formal panel but could mean more informal
                  routes for collecting feedback.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp05" data-section-id="spcp05" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP05: PCP meetings (how many?)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many Parent-Carer Panel meetings have occurred in the relevant quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Give a figure for the relevant quarter. Answer should be numeric only. If no parent-carer-panel meetings have been held, enter 0.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp06" data-section-id="spcp06" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP06: PCP feedback</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Have you used feedback from parents and carers to influence service design and delivery?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Feedback does not need to have been collected through a formal route such as a parent-carer panel. Answer yes if the Local Authority
                  has used feedback collected through any route to demonstrably change any element of service design or delivery.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp06a" data-section-id="spcp06a" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP06a: PCP case study upload</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> You may choose to upload a case study to demonstrate how feedback from parents and carers has informed and influenced your service
                  delivery. If you do so, please use the case study template. This question is optional.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> The case study can relate to any Best Start Family Hub or Healthy Babies service and can be of any length.
                </p>
                <p className="govuk-body">
                  If the case study includes quotes from service users, ensure that all information is anonymised prior to submission. For example, if the quote is attributed to a
                  named individual, you can replace the name with a fake one, and use an age group rather than a specific age, to protect the individual&rsquo;s anonymity.
                </p>
                <p className="govuk-body">
                  Case studies submitted may be shared with other organisations and DHSC or DfE publications related to Best Start Family Hubs and Healthy Babies.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp07" data-section-id="spcp07" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP07: PCP Panel numbers</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many parents/carers are on your Parent Carer Panel?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Give a figure as of the end of the relevant quarter. Answer should be numeric only.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp08" data-section-id="spcp08" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP08: PCP data availability - Sex</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on the sex of members of the Parent Carer Panel?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the biological sex of parent-carer panel members. If you are not able to provide this data, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of parent-carer panel members by:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Female (SPCP08a_female)</li>
                  <li>Male (SPCP08a_male)</li>
                  <li>Other (SPCP08a_other)</li>
                  <li>Prefer not to say (SPCP08a_prefer_not_to_say)</li>
                </ul>
                <p className="govuk-body">The total number of parent-carer panel members across these fields should not exceed the total provided in SPCP07.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="spcp09" data-section-id="spcp09" tabIndex={-1}>
                <h3 className="govuk-heading-m">SPCP09: PCP data availability - Age</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data about the age of panel members?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the age of parent-carer panel members. If you are not able to provide this data, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of parent-carer panel members by the following age groups:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>18-24 (SPCP09a_18_24)</li>
                  <li>25-34 (SPCP09a_25_34)</li>
                  <li>35-44 (SPCP09a_35_44)</li>
                  <li>45-54 (SPCP09a_45_54)</li>
                  <li>55-64 (SPCP09a_55_64)</li>
                  <li>65+ (SPCP09a_65_plus)</li>
                  <li>Prefer not to say (SPCP09a_prefer_not_to_say)</li>
                </ul>
                <p className="govuk-body">The total number of parent-carer panel members across these fields should not exceed the total provided in SPCP07.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pcp10" data-section-id="pcp10" tabIndex={-1}>
                <h3 className="govuk-heading-m">PCP10: PCP data availability - Ethnicity</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on the ethnicity of panel members?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the ethnicity of parent-carer panel members. If you are not able to provide this data, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of parent-carer panel members by the following broad ethnic groups:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>White/White British (SPCP010a_white)</li>
                  <li>Mixed/Multiple ethnic groups (SPCP010a_mixed)</li>
                  <li>Asian/Asian British (SPCP010a_asian)</li>
                  <li>Black/African/Caribbean/Black British (SPCP010a_black)</li>
                  <li>Other ethnic group (SPCP010a_other)</li>
                  <li>Prefer not to say (SPCP010a_prefer_not_to_say)</li>
                </ul>
                <p className="govuk-body">The total number of parent-carer panel members across these fields should not exceed the total provided in SPCP07.</p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Section 2: Quarterly Service Users */}
            <section aria-labelledby="qsu-section-heading">
              <h2 className="govuk-heading-l" id="qsu-section-heading" data-section-id="qsu">
                Section 2: Quarterly Service Users
              </h2>

              <article id="qsu-about" data-section-id="qsu-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">
                  Section 2: Quarterly service users collects information about the users of the services delivered through your Best Start Family Hub network.
                </p>
                <GovUKInsetText>
                  You must have already completed your &lsquo;Section 2: Core Service data&rsquo; module before completing this module. Only services which have been recorded in
                  your core service data will be visible in your quarterly return.
                </GovUKInsetText>
                <p className="govuk-body">
                  Service users refer to all users of the service within the relevant quarter. This includes children, parents, co-parents and other family members or carers,
                  depending on who can access the service and how your data is recorded.
                </p>
                <p className="govuk-body">
                  The purpose of this section is to provide evidence of the reach of services delivered through your Best Start Family Hub services, particularly among
                  disadvantaged communities.
                </p>
                <p className="govuk-body">Tell us about the users who accessed the service over the relevant quarter.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu01" data-section-id="qsu01" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU01: Service status this quarter</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Was the service delivered for users this quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Respond yes if the service was offered to users at least once during the relevant quarter.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu02" data-section-id="qsu02" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU02: User number availability</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many people use this service? This includes centrally collected data or any information received from partners.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have any data on the number of users who accessed the service for the relevant quarter. If you are not able to
                  provide this data, respond no.
                </p>
                <p className="govuk-body">
                  If you answer yes, you will be prompted to provide the breakdown of service users and which types of users are included in your user numbers.
                </p>
                <p className="govuk-body">
                  User data should be de-duplicated, i.e., each unique user should be counted only once, if this can be achieved with the underlying data you have access to. Users
                  should not be counted each time they access the same service within a quarter, if possible. You will be asked to specify whether user numbers have been
                  de-duplicated in a subsequent question.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu05" data-section-id="qsu05" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU05: Number of service users - Overall</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many people used this service over the relevant quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Response should be a numeric whole number including all user types (online, virtual, in-person) for the relevant quarter.
                </p>
                <p className="govuk-body">
                  Your response can include any users who are included in your local data for the service, e.g., children, primary carers, co-parents etc.
                </p>
                <p className="govuk-body">
                  This response will be used to validate any other user breakdowns you provide for this service. For any sub-group of users, the number must be smaller or the same
                  as the total provided here.
                </p>
                <p className="govuk-body">Give a figure for the relevant quarter.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu06" data-section-id="qsu06" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU06: Number of service users - Virtually</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many people used online services over the relevant quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Virtual users refer to users who accessed the service via an online route. This will include for instance, online 1:1 or group peer or
                  co-ordinator led sessions.
                </p>
                <p className="govuk-body">
                  This includes self-directed online access. If the service component being reported against is a website, report virtual users based on unique IP addresses,
                  preferably which spent longer than 2 minutes on the page as this indicates a &lsquo;real&rsquo; user rather than bot-activity or incorrect click-through. If
                  providing website user numbers, make sure that the core service data identifies the service as a website, and do not include website users with in-person users
                  for a service, as website data can dramatically skew user analysis.
                </p>
                <p className="govuk-body">The number of virtual users cannot be greater than the total number of users provided in QSU05.</p>
                <p className="govuk-body">Give a figure for the last 3 months.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu07" data-section-id="qsu07" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU07: De-duplication of service users</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Have you used a method for counting unique individuals who attend this service? For example, are you able to discount repeat visits?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Preferably, aggregate counts of users will refer only to de-duplicated counts, where the same individual attending the same service
                  multiple times during the quarter is not counted each time they access. However, de-duplicated data may not be available for all services you offer. If the data
                  has not been de-duplicated, please indicate this.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu08" data-section-id="qsu08" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU08: Who has been counted</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Which types of users are included in your data?
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Primary carers</li>
                  <li>Co-parents/partners/spouses/other adult</li>
                  <li>Children</li>
                  <li>Siblings</li>
                  <li>Other</li>
                  <li>Unknown</li>
                </ul>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Indicate which types of users are included in your user counts. This data is used to contextualise the scale of your service use.
                  Select all that apply from the list provided.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu04" data-section-id="qsu04" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU04: Service users by ethnic group</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many people used this service in the relevant quarter, by ethnicity?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the ethnicity of service users in broad ethnic groups. If you are not able to provide this data,
                  respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of service users by the following broad ethnic groups:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>White/White British (QSU09_white)</li>
                  <li>Mixed / Multiple ethnic groups (QSU09_mixed)</li>
                  <li>Asian/ Asian British (QSU09_asian)</li>
                  <li>Black / African / Caribbean / Black British (QSU09_black)</li>
                  <li>Other ethnic group (QSU09_other)</li>
                  <li>Prefer not to say (QSU09_prefer_not_to_say)</li>
                </ul>
                <p className="govuk-body">
                  The total number of service users by broad ethnic groups should not exceed the total provided in response to QSU05. If your figures add up to more than in QSU05
                  you will receive a warning message.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu10" data-section-id="qsu10" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU10: Service users by IDACI</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many people used this service in the relevant quarter, by Income Deprivation Affecting Children (IDACI) decile?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the Income Deprivation Affecting Children (IDACI) decile of your service users. If you answer yes, you
                  will be prompted to provide the breakdown of service users by the following deciles:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>1</li>
                  <li>2</li>
                  <li>3</li>
                  <li>4</li>
                  <li>5</li>
                  <li>6</li>
                  <li>7</li>
                  <li>8</li>
                  <li>9</li>
                  <li>10</li>
                </ul>
                <p className="govuk-body">
                  IDACI can be identified by looking up the service user&rsquo;s postcode or Lower Layer Super Output Area (LSOA) against the IDACI database, available here:{' '}
                  <a className="govuk-link" href="https://deprivation.communities.gov.uk/">
                    MHCLG English Indices of Deprivation
                  </a>
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu11" data-section-id="qsu11" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU11: Service users by sex</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many people used this service in the relevant quarter, by biological sex?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the biological sex of service users. If you answer yes, you will be prompted to provide the breakdown
                  of service users by:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Male</li>
                  <li>Female</li>
                  <li>Other</li>
                  <li>Prefer not to say</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu12-age" data-section-id="qsu12-age" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU12: Service users by age</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many people used this service in the relevant quarter, by age?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the age of service users. If you answer yes, you will be prompted to provide the breakdown of service
                  users by:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>0 - 2</li>
                  <li>3 – 4</li>
                  <li>5 – 17</li>
                  <li>18 – 25</li>
                  <li>26 and older</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu13a" data-section-id="qsu13a" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU13A: Service users by age group (availability)</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many people used this service in the relevant quarter, by age group?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the age group of service users. If you are not able to provide this data, respond no.
                </p>
                <p className="govuk-body">Select one:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Yes</li>
                  <li>No</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu03" data-section-id="qsu03" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU03: Pre post availability</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Did the service provider collect pre and post outcome scores for users who accessed this service this quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data for the individual pre and post outcome scores for users who accessed the service during the quarter. If
                  you are not able to provide this data, respond no.
                </p>
                <p className="govuk-body">
                  Pre and post outcome scores cannot be aggregated, they should only be provided where each pre and post score relates to a single individual. If you do not have
                  the data at this level, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the individual service user pre and post scores in another module.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu12-waiting" data-section-id="qsu12-waiting" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU12: Waiting time</h3>
                <GovUKInsetText>
                  Note: the source DHSC guidance reuses the code &lsquo;QSU12&rsquo; for both &lsquo;Service users by age&rsquo; and this &lsquo;Waiting time&rsquo; question. This
                  is likely a typo in the source document and is awaiting confirmation from DHSC.
                </GovUKInsetText>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on waiting times for this service?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> For perinatal mental health, parent infant relationship and infant feeding services you will be asked if you have waiting time data for
                  the service.
                </p>
                <p className="govuk-body">
                  Respond yes if you can provide the waiting time as the average (mean) waiting time in number of working days from referral to receiving a first
                  appointment/session for the reporting period.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu13" data-section-id="qsu13" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU13: Average waiting time</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the average waiting time from referral to first appointment? Provide the average for the last 3 months in days.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Waiting time should be calculated as the average (mean) waiting time in number of days from referral to receiving the first appointment
                  or session, for the service in the last 3 months.
                </p>
                <p className="govuk-body">Provide a numeric response rounded to 1 decimal place, e.g., 3.5.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu15" data-section-id="qsu15" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU15: Clarification comments (removed)</h3>
                <GovUKInsetText>
                  This question (&lsquo;Provide more information about waiting times (optional)&rsquo;) has been removed from the collection and is no longer part of the form. Use
                  QSU21 to provide clarification comments about your service user data.
                </GovUKInsetText>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu18" data-section-id="qsu18" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU18: EBI identification</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Is the service categorised as an Evidence Based Intervention (Parenting support or Early language and Home Learning Environment)?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Select yes if the service comes from the Parenting support or Early language and Home Learning Environment Evidence Based Intervention
                  menu or is an approved Evidence Based Intervention exemption &ndash; as recorded in your core data for services.
                </p>
                <p className="govuk-body">Select no if not.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu19" data-section-id="qsu19" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU19: Attendance rates availability</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> For Evidence Based Interventions only. Do you have data on the total number of sessions service users attended?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> This question will only show if you answered yes to QSU18.
                </p>
                <p className="govuk-body">Select yes if you have the data of the total number of sessions service users attended. Select no if not.</p>
                <p className="govuk-body">
                  Please note, attendance data may not be appropriate to collect for all EBIs. Discuss with your EBI service provider if attendance data is appropriate to collect
                  for this service.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Yes</li>
                  <li>No</li>
                  <li>Not Applicable</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu20" data-section-id="qsu20" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU20: Attendance rates</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many people attended sessions in the relevant quarter, by total attendance rate?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Only provide total attendance figures for users whose Evidence Based Interventions finished in the relevant quarter.
                </p>
                <p className="govuk-body">Do not include attendance data for interventions which were not complete by the end of the reporting period.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qsu21" data-section-id="qsu21" tabIndex={-1}>
                <h3 className="govuk-heading-m">QSU21: Clarification Comments</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Provide more information about your service user data (optional).
                </p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Section 3: Wider Service Users */}
            <section aria-labelledby="wsu-section-heading">
              <h2 className="govuk-heading-l" id="wsu-section-heading" data-section-id="wsu">
                Section 3: Wider Service Users
              </h2>

              <article id="wsu-about" data-section-id="wsu-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <GovUKInsetText>
                  You must have completed your Wider Services Core data prior to completing this section. If you have reported all wider services through your main Services Core
                  data, you will not be required to complete this section.
                </GovUKInsetText>
                <p className="govuk-body">
                  Only include service user numbers for wider service categories that are delivered through your Best Start Family Hub network but do not meet the programme
                  objectives.
                </p>
                <p className="govuk-body">
                  In the portal, you will first be asked to identify which wider service categories you deliver outside of the programme objectives. For those categories only,
                  Section 3 will prompt you to tell us whether or not you have user numbers for that wider service category.
                </p>
                <p className="govuk-body">The wider services available to report against here are:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Mental health services (beyond perinatal mental health and parent-infant relationships)</li>
                  <li>Oral health improvement</li>
                  <li>Stop smoking support</li>
                  <li>Substance misuse support</li>
                  <li>Domestic abuse support</li>
                  <li>Debt advice, money guidance and welfare advice</li>
                  <li>Housing</li>
                  <li>Youth Justice services</li>
                  <li>Youth services &ndash; universal and targeted</li>
                  <li>Food support</li>
                  <li>Social welfare advice services</li>
                  <li>Support for school attendance</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qwsu01" data-section-id="qwsu01" tabIndex={-1}>
                <h3 className="govuk-heading-m">QWSU01: Wider service user number availability</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on user numbers for this wider service category?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have user number data for the wider service category. If you answer yes, you will be prompted to provide the total
                  number of unique service users (QWSU01a).
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="qwsu01a" data-section-id="qwsu01a" tabIndex={-1}>
                <h3 className="govuk-heading-m">QWSU01a: Unique service users</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many unique service users have used wider services in this category?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Provide the number of unique service users using services within that category.
                </p>
                <p className="govuk-body">Do not include any service user numbers that have already been reported as part of the core data.</p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Section 4: Outcome Scores */}
            <section aria-labelledby="pps-section-heading">
              <h2 className="govuk-heading-l" id="pps-section-heading" data-section-id="pps">
                Section 4: Outcome Scores
              </h2>

              <article id="pps-about" data-section-id="pps-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">
                  Section 4: Outcome scores collects information about the outcomes for users of the services delivered through your Best Start Family Hub network.
                </p>
                <GovUKInsetText>
                  You must have already completed your &lsquo;Section 2: Core Service data&rsquo; module and &lsquo;Section 2: Quarterly Service Users&rsquo; before completing this
                  module. Only services which have been designated as capturing data related to individual level outcomes will be visible in this module.
                </GovUKInsetText>
                <p className="govuk-body">
                  Outcome scores refer to the scores pre and post intervention measured using a standardised outcome score measurement tool such as the Public Health Questionnaire
                  (PHQ) – 9, or the Generalised Anxiety and Depression (GAD) – 7, tools. An outcome score measurement tool is a validated, consistent questionnaire used to
                  objectively measure a service users&rsquo; status, progress or symptom severity before, during and after treatment. The most used outcome score measurement tools
                  are suggested in this module, however if a service uses a different measurement tool, you are able to provide data for tools not listed in this module.
                </p>
                <p className="govuk-body">
                  The purpose of this section is to provide evidence of the outcomes from services delivered through your Best Start Family Hub services, particularly among
                  disadvantaged communities.
                </p>
                <p className="govuk-body">
                  Although this module requests individual level data, it is anonymised and we do not request provision of any personally identifiable information such as name,
                  date of birth or address, for the individuals included.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps01" data-section-id="pps01" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS01: Outcome service</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What service does the outcome measure relate to?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> The list of services presented will be those which you have provided core data for and which you have indicated in the quarterly
                  service user module collected pre and post outcome scores.
                </p>
                <p className="govuk-body">
                  If a service is not listed in the drop-down which you expect to see, check that the core and quarterly user data for that service has been entered correctly.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps02" data-section-id="pps02" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS02: Outcome measure</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What outcome measure(s) did this parent or carer complete? Select all which apply.
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>PHQ-9</li>
                  <li>GAD-7</li>
                  <li>SWEMWBS</li>
                  <li>MORS-SF Warmth</li>
                  <li>MORS-SF Invasion</li>
                  <li>ASQ-3</li>
                  <li>KPCS</li>
                  <li>PSS</li>
                  <li>CPRS-SF</li>
                  <li>CPRS-SF Conflict</li>
                  <li>CPRS-SF Closeness</li>
                  <li>SDQ</li>
                  <li>DASS-21</li>
                  <li>Parenting Scale</li>
                  <li>WEMWBS</li>
                  <li>PRFQ</li>
                  <li>Unknown</li>
                  <li>Other</li>
                </ul>
                <p className="govuk-body">
                  <strong>Guidance:</strong> The most commonly used standardised outcome score measures have been provided, select all measures which you have data for, for that
                  user. For instance, if a service user completed both the GAD-7 and PHQ-9, select both of these options.
                </p>
                <p className="govuk-body">
                  If a service user completed an alternative standardised outcome score measure which is not present in the list, select &lsquo;other&rsquo;.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps03" data-section-id="pps03" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS03: Outcome ethnicity</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the user&rsquo;s ethnicity?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If known, provide the user&rsquo;s ethnicity according to their broad ethnic group.
                </p>
                <p className="govuk-body">
                  For all pre and post outcome scores provided for users of a service, the total number of users of each ethnicity, including unknown and prefer not to say should
                  not exceed the total provided in QSU09 for each ethnic group.
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>White or White British</li>
                  <li>Mixed or multiple ethnic groups</li>
                  <li>Asian or Asian British</li>
                  <li>Black, African, Caribbean or Black British</li>
                  <li>Other ethnic group</li>
                  <li>Prefer not to say</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps04" data-section-id="pps04" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS04: Outcome sex</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the user&rsquo;s biological sex?
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Male</li>
                  <li>Female</li>
                  <li>Other</li>
                  <li>Unknown</li>
                  <li>Prefer not to say</li>
                </ul>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If known, provide the user&rsquo;s biological sex.
                </p>
                <p className="govuk-body">
                  For all pre and post outcome scores provided for users of a service, the total number of users of each biological sex, including unknown and prefer not to say,
                  should not exceed the total provided in QSU09 for each sex.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps05" data-section-id="pps05" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS05: Outcome IDACI</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What is the user&rsquo;s IDACI decile?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If known, provide the IDACI decile for the Lower Layer Super Output Area (LSOA) based on the user&rsquo;s postcode. IDACI can be looked
                  up using the data from MHCLG here:{' '}
                  <a className="govuk-link" href="https://deprivation.communities.gov.uk/">
                    MHCLG English Indices of Deprivation
                  </a>
                </p>
                <p className="govuk-body">Select one of the following options:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>1 (most deprived)</li>
                  <li>2</li>
                  <li>3</li>
                  <li>4</li>
                  <li>5</li>
                  <li>6</li>
                  <li>7</li>
                  <li>8</li>
                  <li>9</li>
                  <li>10 (least deprived)</li>
                  <li>Unknown</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps06" data-section-id="pps06" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS06: Pre-intervention PHQ-9</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the 9 item Patient Health Questionnaire (PHQ-9), prior to receiving the service or intervention, provide the
                  score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps07" data-section-id="pps07" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS07: Post-intervention PHQ-9</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the 9 item Patient Health Questionnaire (PHQ-9), subsequently to receiving the service or intervention,
                  provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps08" data-section-id="pps08" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS08: Pre-intervention GAD-7</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the 7 item General Anxiety Disorder (GAD-7) standardised outcome measurement tool, prior to receiving the
                  service or intervention, provide the score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps09" data-section-id="pps09" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS09: Post-intervention GAD-7</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the 7 item General Anxiety Disorder (GAD-7) standardised outcome measurement tool, subsequently to receiving
                  the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps10" data-section-id="pps10" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS10: Pre-intervention SWEMWBS</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Short Warwick-Edinburgh Mental Wellbeing Scale (SWEMWBS) standardised outcome measurement tool, prior to
                  receiving the service or intervention, provide the score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps11" data-section-id="pps11" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS11: Post-intervention SWEMWBS</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Short Warwick-Edinburgh Mental Wellbeing Scale (SWEMWBS) standardised outcome measurement tool,
                  subsequently to receiving the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps12" data-section-id="pps12" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS12: Pre-intervention MORS-SF</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Mothers Object Relations Scales Short Form (MORS-SF) standardised outcome measurement tool, prior to
                  receiving the service or intervention, provide the score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps13" data-section-id="pps13" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS13: Post-intervention MORS-SF</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Mothers Object Relations Scales Short Form (MORS-SF) standardised outcome measurement tool, subsequently
                  to receiving the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps14" data-section-id="pps14" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS14: Pre-intervention ASQ-3</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was the child&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Ages and Stages Questionnaire, Third edition (ASQ-3) developmental screening tool, prior to receiving the
                  service or intervention, provide the score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps15" data-section-id="pps15" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS15: Post-intervention ASQ-3</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this child&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Ages and Stages Questionnaire, Third edition (ASQ-3) developmental screening tool, subsequently to
                  receiving the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps16" data-section-id="pps16" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS16: Pre-intervention KPCS</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Karitane Parenting Confidence Scale (KPCS) standardised parenting confidence measurement tool prior to
                  receiving the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">Note the Karitane Parenting Confidence Scale is for parents of children aged 0-12 months.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps17" data-section-id="pps17" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS17: Post-intervention KPCS</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Karitane Parenting Confidence Scale (KPCS) standardised parenting confidence measurement tool
                  subsequently to receiving the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps18" data-section-id="pps18" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS18: Pre-intervention HLE</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed a standardised measurement tool for assessing the Home Learning Environment prior to receiving the service or
                  intervention, provide the score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps19" data-section-id="pps19" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS19: Post-intervention HLE</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed a standardised measurement tool for assessing the Home Learning Environment subsequently to receiving the
                  service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps20" data-section-id="pps20" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS20: Pre-intervention PSS</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the 10 item Perceived Stress Scale (PSS-10) standardised stress measurement tool prior to receiving the
                  service or intervention, provide the score here.
                </p>
                <p className="govuk-body">Note the PSS-10 is for young people and adults aged 12 and above.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps22" data-section-id="pps22" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS22: Post-intervention PSS</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the 10 item Perceived Stress Scale (PSS-10) standardised stress measurement tool subsequently to receiving
                  the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps23" data-section-id="pps23" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS23: Pre-intervention CPRS-SF</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Child-Parent Relationship Scale Short-Form (CPRS-SF) standardised parenting measurement tool prior to
                  receiving the service or intervention, provide the score here.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps24" data-section-id="pps24" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS24: Post-intervention CPRS-SF</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Child-Parent Relationship Scale Short-Form (CPRS-SF) standardised parenting measurement tool subsequently
                  to receiving the service or intervention, provide the score here.
                </p>
                <p className="govuk-body">
                  The post-intervention score can be recorded at any time after a service user has engaged with a service but is typically completed within 6 months of the
                  intervention.
                </p>
                <p className="govuk-body">
                  Provide the pre- and post- score in the quarter in which the post-score was recorded. I.e., if the post score was recorded after the cut-off date for the end of a
                  quarter, provide both pre and post score for the individual in the following quarter return.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps19-sdq" data-section-id="pps19-sdq" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS19: Pre/Post-intervention SDQ</h3>
                <GovUKInsetText>
                  Note: the source DHSC guidance reuses the code &lsquo;PPS19&rsquo; for both this &lsquo;SDQ&rsquo; question and &lsquo;Post-intervention HLE&rsquo; above. This
                  guide uses the anchor &lsquo;pps19-sdq&rsquo; to keep the two distinct until DHSC confirms the correct codes.
                </GovUKInsetText>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention / post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Strengths and Difficulties Questionnaire (SDQ) prior to receiving the service or intervention, provide
                  the pre-intervention score. If the user has completed the SDQ after receiving the service or intervention, provide the post-intervention score.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps20-dass21" data-section-id="pps20-dass21" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS20: Pre/Post-intervention DASS-21</h3>
                <GovUKInsetText>
                  Note: the source DHSC guidance reuses the code &lsquo;PPS20&rsquo; for both this &lsquo;DASS-21&rsquo; question and &lsquo;Pre-intervention PSS&rsquo; above. This
                  guide uses the anchor &lsquo;pps20-dass21&rsquo; to keep the two distinct until DHSC confirms the correct codes.
                </GovUKInsetText>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention / post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Depression Anxiety Stress Scale &ndash; Short Form (DASS-21) prior to receiving the service or
                  intervention, provide the pre-intervention score. If the user has completed the DASS-21 after receiving the service or intervention, provide the post-intervention
                  score.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps21" data-section-id="pps21" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS21: Pre/Post-intervention Parenting Scale</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention / post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user has completed the Parenting Scale prior to receiving the service or intervention, provide the pre-intervention score. If
                  the user has completed the Parenting Scale after receiving the service or intervention, provide the post-intervention score.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps25" data-section-id="pps25" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS25: Pre-intervention other outcome measure</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score pre-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user completed an alternative standardised outcome measurement tool not included in the list provided, provide the detail of the
                  tool used and the user&rsquo;s score prior to receiving the intervention.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps26" data-section-id="pps26" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS26: Post-intervention other outcome measure</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What was this parent or carer&rsquo;s score post-intervention?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If the user completed an alternative standardised outcome measurement tool not included in the list provided, provide the detail of the
                  tool used and the user&rsquo;s score after receiving the intervention.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps27" data-section-id="pps27" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS27: Outcome case study</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Would you like to upload a case study (optional)? If you have evidence or examples of approaches which have improved user outcomes, you
                  can upload them here. For example, you might like to tell us about how you have brought different services together to create better outcomes or how you have
                  tested new approaches. Please use the case study template to create your case study.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> This provides an opportunity to upload a case study from your Local Authority, if you wish to share an example of recent practice which
                  has resulted in improved user outcomes.
                </p>
                <p className="govuk-body">The case study can be any length.</p>
                <p className="govuk-body">
                  If the case study includes quotes from service users, ensure that all information is anonymised prior to submission. For example, if the quote is attributed to a
                  named individual, you can replace the name with a fake one and use an age group rather than a specific age, to protect the individual&rsquo;s anonymity.
                </p>
                <p className="govuk-body">
                  Case studies submitted may be shared with other organisations and DHSC or DfE publications related to Best Start Family Hubs and Healthy Babies.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="pps28" data-section-id="pps28" tabIndex={-1}>
                <h3 className="govuk-heading-m">PPS28: Clarification comments</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Provide more information about outcome scores (optional).
                </p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Section 5: Breastfeeding Rates */}
            <section aria-labelledby="br-section-heading">
              <h2 className="govuk-heading-l" id="br-section-heading" data-section-id="br">
                Section 5: Breastfeeding Rates
              </h2>

              <article id="br-about" data-section-id="br-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <p className="govuk-body">This section asks you to provide an average breastfeeding rate at initiation and 6-8 weeks.</p>
                <p className="govuk-body">
                  For breastfeeding data at 6-8 weeks, we recognise that this is already routinely returned to central government. We are requesting this as part of the MI to give
                  us a timelier picture of breastfeeding outcomes in your area. This data is intended for use as an early indication of how breastfeeding rates are changing and
                  does not replace data collected elsewhere in published statistics.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="br01" data-section-id="br01" tabIndex={-1}>
                <h3 className="govuk-heading-m">BR01: Breastfeeding rates at initiation</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What were the average breastfeeding rates at initiation over the last quarter (optional)? Provide an overall figure for your local
                  authority, if you have one.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If you have the data to be able to do so, calculate the rates for initiation as (totally + partially breastfed infants) divided by the
                  total number of infants born during the quarter multiplied by 100.
                </p>
                <p className="govuk-body">
                  If you do not have this data and need to use a different base population, such as the number of infants being breastfed at an earlier stage, specify the base
                  population you have used in the clarification comments.
                </p>
                <p className="govuk-body">If you provide a rate and do not provide any further clarification comments, we will assume that you have used the method requested.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="br02" data-section-id="br02" tabIndex={-1}>
                <h3 className="govuk-heading-m">BR02: Breastfeeding rates at 6-8 weeks</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> What were the average breastfeeding rates at 6-8 weeks over the last quarter (optional)? Provide an overall figure for your local
                  authority, if you have one.
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> If you have the data to be able to do so, calculate the rates for 6-8 weeks as (totally + partially breastfed infants) divided by the
                  total number of infants aged 6-8 weeks during the quarter multiplied by 100.
                </p>
                <p className="govuk-body">
                  If you do not have this data and need to use a different base population, such as the number of infants being breastfed at an earlier stage, specify the base
                  population you have used in the clarification comments.
                </p>
                <p className="govuk-body">If you provide a rate and do not provide any further clarification comments, we will assume that you have used the method requested.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="br03" data-section-id="br03" tabIndex={-1}>
                <h3 className="govuk-heading-m">BR03: Comments</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Provide more information about breastfeeding rates (optional).
                </p>
              </article>
            </section>

            <hr className="govuk-section-break govuk-section-break--xl govuk-section-break--visible" />

            {/* Section 6: Hub Users */}
            <section aria-labelledby="bsfh-section-heading">
              <h2 className="govuk-heading-l" id="bsfh-section-heading" data-section-id="bsfh">
                Section 6: Hub Users
              </h2>

              <article id="bsfh-about" data-section-id="bsfh-about" tabIndex={-1}>
                <h3 className="govuk-heading-m">About this section</h3>
                <GovUKInsetText>
                  Please note that bulk upload functionality is not available for this section. An offline version of the question set in an excel template has been provided to
                  view the questions in advance of the portal becoming available.
                </GovUKInsetText>
                <p className="govuk-body">Please only report hub user numbers for locations you classed as Best Start Family Hubs in your Core Site data section.</p>
                <p className="govuk-body">
                  We define &lsquo;hub user&rsquo; as someone who has accessed your Best Start Family Hubs for any reason, including non-programme related activity, such as seeing
                  a health visitor, or attending a non-programme funded service.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh01" data-section-id="bsfh01" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH01: Hub user number availability</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many users accessed your Best Start Family Hubs?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Select the option which best reflects your data availability for the relevant quarter:
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Yes &ndash; you have user data from at least one Best Start Family Hub in your area.</li>
                  <li>No &ndash; you do not have user data available for any hubs.</li>
                </ul>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh02" data-section-id="bsfh02" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH02: Which Best Start Family Hubs can you provide user numbers for</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Which Best Start Family Hubs can you provide user numbers for?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> This question will only be shown if you answered &lsquo;Yes&rsquo; to BSFH01.
                </p>
                <p className="govuk-body">Select one of:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>All Best Start Family Hubs</li>
                  <li>Some Best Start Family Hubs</li>
                </ul>
                <p className="govuk-body">Tell us if you have user data for all or some of your Best Start Family Hubs.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh02a" data-section-id="bsfh02a" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH02A: Specify which Best Start Family Hubs can you provide user numbers for</h3>
                <p className="govuk-body">
                  <strong>Guidance:</strong> This question will only be shown if you answered &lsquo;Some Best Start Family Hubs&rsquo; to BSFH02.
                </p>
                <p className="govuk-body">
                  Tell us the names of the Best Start Family Hubs for which you can provide user numbers for the relevant quarter. Please enter hub names separated by commas. Names
                  must match the name given in core data &ndash; delivery locations.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh03" data-section-id="bsfh03" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH03: Number of hub users - Overall</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many users have accessed your Best Start Family Hubs in the relevant quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> This question will only be shown if you answered &lsquo;yes&rsquo; to BSFH01.
                </p>
                <p className="govuk-body">
                  Provide the total number of users who accessed your Best Start Family Hubs during the relevant quarter. For example, if an individual has checked into your Best
                  Start Family Hub, and brought with them a child and another adult, please report the total number of users. In this example, the total number of users would be 3.
                </p>
                <p className="govuk-body">The response should be a numeric whole number.</p>
                <p className="govuk-body">Include all user types recorded in your data (e.g. children, parents, carers).</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh04" data-section-id="bsfh04" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH04: De-duplication of hub users</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have a method for counting unique users who access your Best Start Family Hubs? For example, are you able to discount repeat
                  visits?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you can provide data on unique users (i.e. repeat visits by the same person to the same location within the quarter are
                  not counted multiple times). If you cannot provide data on unique users, which excludes things like repeat visits, respond no.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh05" data-section-id="bsfh05" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH05: Who has been counted</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Which types of user are included in your data?
                </p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Primary carer</li>
                  <li>Co-parents/partners/spouse/other</li>
                  <li>Adult</li>
                  <li>Child</li>
                  <li>Sibling</li>
                  <li>Other</li>
                  <li>Unknown</li>
                </ul>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Select all user types that are included in your reported numbers. This information is used to understand which groups are represented
                  in your totals. Select all that apply based on your data.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh06" data-section-id="bsfh06" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH06: Hub users by location availability</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many users accessed your Best Start Family Hubs, by each hub location, in the relevant quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data on how many users accessed your Best Start Family Hubs by each hub location during the relevant quarter. If
                  no data is available, respond no.
                </p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh07" data-section-id="bsfh07" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH07: Hub users by location</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> How many users accessed your Best Start Family Hubs, by each hub location, in the relevant quarter?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Provide the number of users for each hub location. Where possible, this should represent unique individuals (de-duplicated) rather than
                  total visits. You will need to write the name of the Best Start Family Hub and the number of users next to it.
                </p>
                <p className="govuk-body">The total across all locations must not exceed the overall total reported in BSFH03.</p>
                <p className="govuk-body">The response should be a numeric whole number.</p>
                <p className="govuk-body">Include all user types recorded in your data (e.g. children, parents, carers).</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh08" data-section-id="bsfh08" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH08: Hub users by ethnicity</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many users accessed your Best Start Family Hubs in the relevant quarter, by ethnicity?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data on the ethnicity of users accessing your Best Start Family Hubs during the relevant quarter. If no data is
                  available, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of service users by:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>White or White British (BSFH09_white)</li>
                  <li>Mixed or multiple ethnic groups (BSFH09_multiple)</li>
                  <li>Asian or Asian British (BSFH09_asian)</li>
                  <li>Black, African, Caribbean or Black British (BSFH09_black)</li>
                  <li>Other ethnic group (BSFH09_other)</li>
                  <li>Unknown (BSFH09_unknown)</li>
                  <li>Prefer not to say (BSFH09_prefer_not_to_say)</li>
                </ul>
                <p className="govuk-body">Provide the number of users in each broad ethnic group. Where possible, this should represent unique individuals (de-duplicated).</p>
                <p className="govuk-body">The total across all ethnic groups must not exceed the total in BSFH03.</p>
                <p className="govuk-body">
                  Totals may be lower where data is incomplete. Users with missing information should be recorded under &lsquo;Unknown&rsquo; (or &lsquo;Prefer not to say&rsquo;
                  where applicable).
                </p>
                <p className="govuk-body">Response should be in numeric whole numbers.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh10" data-section-id="bsfh10" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH10: Hub users by IDACI</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many users accessed your Best Start Family Hubs in the relevant quarter, by IDACI decile?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data on the Income Deprivation Affecting Children (IDACI) decile of users. If no data is available, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of service users by:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>IDACI 1 (BSFH11_IDACI1)</li>
                  <li>IDACI 2 (BSFH11_IDACI2)</li>
                  <li>IDACI 3 (BSFH11_IDACI3)</li>
                  <li>IDACI 4 (BSFH11_IDACI4)</li>
                  <li>IDACI 5 (BSFH11_IDACI5)</li>
                  <li>IDACI 6 (BSFH11_IDACI6)</li>
                  <li>IDACI 7 (BSFH11_IDACI7)</li>
                  <li>IDACI 8 (BSFH11_IDACI8)</li>
                  <li>IDACI 9 (BSFH11_IDACI9)</li>
                  <li>IDACI 10 (BSFH11_IDACI10)</li>
                  <li>IDACI Unknown (BSFH11_IDACIUNKNOWN)</li>
                </ul>
                <p className="govuk-body">Where possible, this should represent unique individuals (de-duplicated).</p>
                <p className="govuk-body">
                  IDACI can be identified by looking up the service user&rsquo;s postcode or Lower Layer Super Output Area (LSOA) against the IDACI database, available here:{' '}
                  <a className="govuk-link" href="https://deprivation.communities.gov.uk/">
                    MHCLG English Indices of Deprivation
                  </a>
                  .
                </p>
                <p className="govuk-body">Totals must not exceed the overall total in BSFH03 but may be lower where IDACI data is not available.</p>
                <p className="govuk-body">Response should be in numeric whole numbers.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh12" data-section-id="bsfh12" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH12: Hub users by sex</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many users accessed your Best Start Family Hubs in the relevant quarter, by biological sex?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data on the biological sex of users accessing your hubs. If no data is available, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of service users by:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>Female (BSFH13_female)</li>
                  <li>Male (BSFH13_male)</li>
                  <li>Other (BSFH13_other)</li>
                  <li>Prefer not to say (BSFH13_prefer_not_to_say)</li>
                  <li>Unknown (BSFH13_unknown)</li>
                </ul>
                <p className="govuk-body">Provide the number of users for each category. Where possible, this should represent unique individuals (de-duplicated).</p>
                <p className="govuk-body">The total must not exceed the overall number of users (BSFH03).</p>
                <p className="govuk-body">
                  Totals may be lower where data is incomplete. Users with missing information should be recorded under &lsquo;Unknown&rsquo; or &lsquo;Prefer not to say&rsquo;
                  where applicable.
                </p>
                <p className="govuk-body">Response should be in numeric whole numbers.</p>
              </article>
              <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

              <article id="bsfh14" data-section-id="bsfh14" tabIndex={-1}>
                <h3 className="govuk-heading-m">BSFH14: Hub users by age</h3>
                <p className="govuk-body">
                  <strong>Question:</strong> Do you have data on how many users accessed your Best Start Family Hubs in the relevant quarter, by age group?
                </p>
                <p className="govuk-body">
                  <strong>Guidance:</strong> Answer yes if you have data on users by age group. If no data is available, respond no.
                </p>
                <p className="govuk-body">If you answer yes, you will be prompted to provide the breakdown of service users by:</p>
                <ul className="govuk-list govuk-list--bullet">
                  <li>0 &ndash; 2 years (BSFH15_02)</li>
                  <li>3 &ndash; 5 years (BSFH15_35)</li>
                  <li>6 &ndash; 18 years (BSFH15_618)</li>
                  <li>19 &ndash; 25 years (BSFH15_1925)</li>
                  <li>26 and older (BSFH15_26)</li>
                  <li>Unknown (BSFH15_unknown)</li>
                  <li>Prefer not to say (BSFH15_prefer_not_to_say)</li>
                </ul>
                <p className="govuk-body">Provide the number of users for each age group. Where possible, this should represent unique individuals (de-duplicated).</p>
                <p className="govuk-body">The total must not exceed the overall number of users (BSFH03).</p>
                <p className="govuk-body">
                  Totals may be lower where data is incomplete. Users with missing information should be recorded under &lsquo;Unknown&rsquo; or &lsquo;Prefer not to say&rsquo;
                  where applicable.
                </p>
              </article>
            </section>

            <div className="govuk-!-margin-top-8">
              <a href="#main-content" className="govuk-link">
                Back to top
              </a>
            </div>
          </div>
        </div>
      </div>
    </GeneralLayout>
  );
};

export default KnowledgeHub;
