import React from 'react';

const SpreadsheetIcon = (): React.ReactElement => (
  <svg width="109" height="150" viewBox="0 0 109 150" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
    <rect width="109" height="150" fill="#F3F2F1"/>
    <g filter="url(#filter0_d_spreadsheet)">
      <rect width="99" height="140" transform="translate(5 5)" fill="white"/>
      <path d="M17 17H92V44H17V17ZM17 64H35.75V127H17V64ZM72 66V125H56V66H72ZM74 64H54V127H74V64Z" fill="#A8ABAD"/>
      <path d="M54 66.05V125H37.8V66.05H54ZM56 64.05H35.75V127.05H56V64V64.05ZM90 66.05V125H74.05V66.05H90ZM92 64.05H72V127.05H92V64V64.05Z" fill="#A8ABAD"/>
    </g>
    <defs>
      <filter id="filter0_d_spreadsheet" x="3" y="5" width="103" height="144" filterUnits="userSpaceOnUse" colorInterpolationFilters="sRGB">
        <feFlood floodOpacity="0" result="BackgroundImageFix"/>
        <feColorMatrix in="SourceAlpha" type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0" result="hardAlpha"/>
        <feOffset dy="2"/>
        <feGaussianBlur stdDeviation="1"/>
        <feColorMatrix type="matrix" values="0 0 0 0 0.694118 0 0 0 0 0.705882 0 0 0 0 0.713726 0 0 0 1 0"/>
        <feBlend mode="normal" in2="BackgroundImageFix" result="effect1_dropShadow"/>
        <feBlend mode="normal" in="SourceGraphic" in2="effect1_dropShadow" result="shape"/>
      </filter>
    </defs>
  </svg>
);

export default SpreadsheetIcon;
