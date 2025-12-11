<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes"/>
  <xsl:template match="/">
    <contacts>
      <xsl:for-each select="contacts/contact">
        <xsl:sort select="lastName"/>
        <contact id="{@id}">
          <firstName><xsl:value-of select="firstName"/></firstName>
          <lastName><xsl:value-of select="lastName"/></lastName>
          <phone><xsl:value-of select="phone"/></phone>
          <email><xsl:value-of select="email"/></email>
        </contact>
      </xsl:for-each>
    </contacts>
  </xsl:template>
</xsl:stylesheet>

